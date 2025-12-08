using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Codexus.Cipher.Entities.WPFLauncher;
using Codexus.Cipher.Protocol;
using Codexus.Development.SDK.Entities;
using Codexus.Development.SDK.Manager;
using OpenNEL.Entities.Web;
using OpenNEL.Entities.Web.NEL;
using Serilog;

namespace OpenNEL.Manager;

    public class UserManager : IUserManager
    {
	private const string UsersFilePath = "users.json";

	private const int ExpirationMinutes = 30;

	private const int CheckIntervalMs = 2000;

	private const int SaveDebounceMs = 1000;

	private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
	{
		WriteIndented = true
	};

	private static readonly SemaphoreSlim InstanceLock = new SemaphoreSlim(1, 1);

	private static UserManager? _instance;

	private readonly ConcurrentDictionary<string, EntityUser> _users = new ConcurrentDictionary<string, EntityUser>();

	private readonly ConcurrentDictionary<string, EntityAvailableUser> _availableUsers = new ConcurrentDictionary<string, EntityAvailableUser>();

	private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

	private readonly SemaphoreSlim _saveSemaphore = new SemaphoreSlim(1, 1);

	private volatile bool _isDirty;

        private Timer? _saveTimer;

        public event Action? UsersReadFromDisk;

	public static UserManager Instance
	{
		get
		{
			if (_instance != null)
			{
				return _instance;
			}
			InstanceLock.Wait();
			try
			{
				return _instance ?? (_instance = new UserManager());
			}
			finally
			{
				InstanceLock.Release();
			}
		}
	}

	private UserManager()
	{
		IUserManager.Instance = this;
		InitializeSaveTimer();
		Task.Run((Func<Task?>)MaintainThreadAsync, _cancellationTokenSource.Token);
	}

	private void InitializeSaveTimer()
	{
		_saveTimer = new Timer(async delegate
		{
			try
			{
				await SaveUsersToDiskIfDirtyAsync();
			}
			catch (Exception)
			{
			}
		}, null, -1, -1);
	}

	public EntityAvailableUser? GetAvailableUser(string entityId)
	{
		if (!_availableUsers.TryGetValue(entityId, out EntityAvailableUser value))
		{
			return null;
		}
		return value;
	}

	private async Task MaintainThreadAsync()
	{
		using WPFLauncher launcher = new WPFLauncher();
		_ = 2;
		try
		{
			while (!_cancellationTokenSource.Token.IsCancellationRequested)
			{
				try
				{
					await ProcessExpiredUsersAsync(launcher);
					await Task.Delay(2000, _cancellationTokenSource.Token);
				}
				catch (OperationCanceledException)
				{
					break;
				}
				catch (Exception exception)
				{
                    Log.Error(exception, "维护线程迭代错误");
					await Task.Delay(2000);
				}
			}
		}
		catch (OperationCanceledException)
		{
            Log.Information("维护线程已取消");
		}
		catch (Exception exception2)
		{
            Log.Error(exception2, "维护线程发生致命错误");
		}
	}

	private async Task ProcessExpiredUsersAsync(WPFLauncher launcher)
	{
		long expirationThreshold = DateTimeOffset.UtcNow.AddMinutes(-30.0).ToUnixTimeMilliseconds();
		List<EntityAvailableUser> list = _availableUsers.Values.Where((EntityAvailableUser u) => u.LastLoginTime < expirationThreshold).ToList();
		if (list.Count != 0)
		{
			await Task.WhenAll(list.Select((EntityAvailableUser user) => UpdateExpiredUserAsync(user, launcher)));
		}
	}

	private static async Task UpdateExpiredUserAsync(EntityAvailableUser expiredUser, WPFLauncher launcher)
	{
		try
		{
			EntityAuthenticationUpdate entityAuthenticationUpdate = await launcher.AuthenticationUpdateAsync(expiredUser.UserId, expiredUser.AccessToken);
            if (entityAuthenticationUpdate == null || entityAuthenticationUpdate.Token == null)
            {
                Log.Error("更新用户 {UserId} 的令牌失败", expiredUser.UserId);
                return;
            }
			expiredUser.AccessToken = entityAuthenticationUpdate.Token;
			expiredUser.LastLoginTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            Log.Information("用户 {UserId} 的令牌已成功更新", expiredUser.UserId);
		}
		catch (Exception exception)
		{
            Log.Error(exception, "更新用户 {UserId} 时发生错误", expiredUser.UserId);
		}
	}

	public List<EntityAccount> GetAvailableUsers()
	{
		Dictionary<string, EntityUser> userLookup = _users.ToDictionary<KeyValuePair<string, EntityUser>, string, EntityUser>((KeyValuePair<string, EntityUser> kvp) => kvp.Key, (KeyValuePair<string, EntityUser> kvp) => kvp.Value);
		EntityUser value;
		return _availableUsers.Values.Select((EntityAvailableUser available) => new EntityAccount
		{
			UserId = available.UserId,
			Alias = (userLookup.TryGetValue(available.UserId, out value) ? value.Alias : string.Empty)
		}).ToList();
	}

	public List<EntityUser> GetUsersNoDetails()
	{
		return _users.Values.Select((EntityUser u) => new EntityUser
		{
			UserId = u.UserId,
			Authorized = u.Authorized,
			AutoLogin = false,
			Channel = u.Channel,
			Type = u.Type,
			Details = "",
			Platform = u.Platform,
			Alias = u.Alias
		}).ToList();
	}

	public EntityUser? GetUserByEntityId(string entityId)
	{
		if (!_users.TryGetValue(entityId, out EntityUser value))
		{
			return null;
		}
		return value;
	}

    public EntityAvailableUser? GetLastAvailableUser()
    {
        return _availableUsers.Values.OrderBy(u => u.LastLoginTime).LastOrDefault();
    }

	public void AddUserToMaintain(EntityAuthenticationOtp authenticationOtp)
	{
		ArgumentNullException.ThrowIfNull(authenticationOtp, "authenticationOtp");
		EntityAvailableUser addValue = new EntityAvailableUser
		{
			UserId = authenticationOtp.EntityId,
			AccessToken = authenticationOtp.Token,
			LastLoginTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
		};
		_availableUsers.AddOrUpdate(authenticationOtp.EntityId, addValue, delegate(string _, EntityAvailableUser existing)
		{
			existing.AccessToken = authenticationOtp.Token;
			existing.LastLoginTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
			return existing;
		});
	}

	public void AddUser(EntityUser entityUser, bool saveToDisk = true)
	{
		ArgumentNullException.ThrowIfNull(entityUser, "entityUser");
		_users.AddOrUpdate(entityUser.UserId, entityUser, delegate(string _, EntityUser existing)
		{
			existing.Authorized = true;
			return existing;
		});
		if (saveToDisk)
		{
			MarkDirtyAndScheduleSave();
		}
	}

	public void RemoveUser(string entityId)
	{
		if (_users.TryRemove(entityId, out EntityUser _))
		{
			MarkDirtyAndScheduleSave();
		}
	}

        public void RemoveAvailableUser(string entityId)
        {
		_availableUsers.TryRemove(entityId, out EntityAvailableUser _);
		if (_users.TryGetValue(entityId, out EntityUser value2))
		{
			value2.Authorized = false;
			MarkDirtyAndScheduleSave();
		}
	}

        public async Task ReadUsersFromDiskAsync()
        {
            try
            {
                if (!File.Exists(UsersFilePath))
                {
                    Log.Information("未找到用户文件，使用空的用户列表启动");
                    UsersReadFromDisk?.Invoke();
                    return;
                }
                List<EntityUser> list = JsonSerializer.Deserialize<List<EntityUser>>(await File.ReadAllTextAsync(UsersFilePath)) ?? new List<EntityUser>();
                _users.Clear();
                foreach (EntityUser item in list)
                {
                    item.Authorized = false;
                    _users.TryAdd(item.UserId, item);
                }
                Log.Information("从磁盘加载了 {Count} 个用户", list.Count);
                UsersReadFromDisk?.Invoke();
            }
            catch (Exception exception)
            {
                Log.Error(exception, "读取磁盘上的用户时发生错误");
                _users.Clear();
                UsersReadFromDisk?.Invoke();
            }
        }

        public void UpdateUserAlias(string entityId, string alias)
        {
            if (string.IsNullOrWhiteSpace(entityId)) return;
            if (_users.TryGetValue(entityId, out var user))
            {
                user.Alias = alias ?? string.Empty;
                MarkDirtyAndScheduleSave();
            }
        }

	public void ReadUsersFromDisk()
	{
		ReadUsersFromDiskAsync().GetAwaiter().GetResult();
	}

	public void MarkDirtyAndScheduleSave()
	{
		_isDirty = true;
		_saveTimer?.Change(1000, -1);
	}

	private async Task SaveUsersToDiskIfDirtyAsync()
	{
		if (!_isDirty)
		{
			return;
		}
		await _saveSemaphore.WaitAsync();
		try
		{
			if (_isDirty)
			{
				List<EntityUser> usersList = _users.Values.ToList();
				string contents = JsonSerializer.Serialize(usersList, JsonOptions);
				await File.WriteAllTextAsync(UsersFilePath, contents);
				_isDirty = false;
                Log.Debug("已将 {Count} 个用户保存到磁盘", usersList.Count);
			}
		}
		catch (Exception exception)
		{
            Log.Error(exception, "保存用户到磁盘时发生错误");
			throw;
		}
		finally
		{
			_saveSemaphore.Release();
		}
	}

	public async Task SaveUsersToDiskAsync()
	{
		_isDirty = true;
		await SaveUsersToDiskIfDirtyAsync();
	}

	public void SaveUsersToDisk()
	{
		SaveUsersToDiskAsync().GetAwaiter().GetResult();
	}
}
