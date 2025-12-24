using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using OpenNEL.SDK.Entities;
using Serilog;

namespace OpenNEL.SDK.Utils;

public sealed class WebNexusApi : IDisposable
{
	private const string BaseUrl = "https://abc.abdsfa.cac";

	private readonly HttpClient _httpClient;

	private readonly Action<string, int> _progressCallback;

	private bool _disposed;

	public WebNexusApi(string nexusToken, Action<string, int>? progressCallback = null)
	{
		_httpClient = new HttpClient
		{
			BaseAddress = new Uri(BaseUrl)
		};
		_httpClient.DefaultRequestHeaders.Accept.Clear();
		_httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
		_httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + nexusToken);
		_progressCallback = progressCallback ?? ((Action<string, int>)delegate
		{
		});
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	public async Task<string> ComputeAuthenticationBodyAsync(string serverId, long gameId, string gameVersion, string modInfo, string channel, int userId, string handshakeKey)
	{
		var requestData = new { serverId, gameId, gameVersion, modInfo, channel, userId, handshakeKey };
		return await PostAsync("/api/GameCipher/compute/authentication/body", requestData);
	}

	public async Task<string> ComputeHandshakeBodyAsync(int userId, string userToken, string base64Context, string channel, string gameVersion)
	{
		var requestData = new { userId, userToken, base64Context, channel, gameVersion };
		return await PostAsync("/api/GameCipher/compute/authentication/handshake", requestData);
	}

	public async Task<string> PeAccountConvert(string body)
	{
		var requestData = new { body };
		return await PostAsync("/api/PeGameCipher/account/convert", requestData);
	}

	public async Task<string> PeAuthentication(string clientKey, string displayName, string serverId, string gameType, uint userId, string userToken)
	{
		var requestData = new { clientKey, displayName, serverId, gameType, userId, userToken };
		return await PostAsync("/api/PeGameCipher/authentication", requestData);
	}

	public async Task<string> PeHttpEncryptAsync(string body)
	{
		var requestData = new { body };
		return await PostAsync("/api/PeGameCipher/crypto/encrypt", requestData);
	}

	public async Task<string> PeHttpDecryptAsync(string body)
	{
		var requestData = new { body };
		return await PostAsync("/api/PeGameCipher/crypto/decrypt", requestData);
	}

	public string PeMcpGetCheckNum(string dynamicPyCode, string dynamicCheckSalt, string gamePlayerId)
	{
		var requestData = new { dynamicPyCode, dynamicCheckSalt, gamePlayerId };
		return JsonSerializer.Deserialize<BodyIn>(PostAsync("/api/PeZeroKnowledgeProof/zkp/get/check-num", requestData).GetAwaiter().GetResult()).Body;
	}

	public string PeMcpGetStartType(string signature, string userId)
	{
		var requestData = new { signature, userId };
		return JsonSerializer.Deserialize<BodyIn>(PostAsync("/api/PeZeroKnowledgeProof/zkp/get/start-type", requestData).GetAwaiter().GetResult()).Body;
	}

	public IdCard GetRandomIdCard()
	{
		return JsonSerializer.Deserialize<IdCard>(GetAsync("/api/app/get/app/id-card").GetAwaiter().GetResult());
	}

	public string ComputeCaptchaAsync(byte[] image)
	{
		var requestData = new
		{
			body = Convert.ToBase64String(image)
		};
		return JsonSerializer.Deserialize<BodyIn>(PostAsync("/api/app/compute/captcha", requestData).GetAwaiter().GetResult()).Body;
	}

	public async Task<string> GetAppVersionAsync(string appId, string appSecret)
	{
		var requestData = new { appId, appSecret };
		return await PostAsync("/api/app/get/app/version", requestData);
	}

	public byte[] DownloadFile(string url, string pluginId)
	{
		return DownloadFileAsync(url, pluginId).GetAwaiter().GetResult();
	}

	private async Task<string> PostAsync<T>(string endpoint, T requestData, Dictionary<string, string>? headers = null)
	{
		try
		{
			StringContent stringContent = new StringContent(JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json");
			if (headers != null)
			{
				foreach (KeyValuePair<string, string> header in headers)
				{
					stringContent.Headers.Add(header.Key, header.Value);
				}
			}
			HttpResponseMessage response = await _httpClient.PostAsync(endpoint, stringContent);
			string text = await response.Content.ReadAsStringAsync();
			if (response.StatusCode == HttpStatusCode.Unauthorized)
			{
				Log.Warning("Warning: Your subscription time has expired, or the AccessToken has expired, try refreshing.", Array.Empty<object>());
			}
			if (response.IsSuccessStatusCode)
			{
				return text;
			}
			Log.Error("Request failed with status code {StatusCode}. Response: {Data}", new object[2] { response.StatusCode, text });
			throw new Exception($"Request failed with status code {response.StatusCode}. Response: {text}");
		}
		catch (HttpRequestException)
		{
			throw;
		}
		catch (Exception ex2)
		{
			Exception ex3 = ex2;
			throw new Exception("Error processing API response: " + ex3.Message);
		}
	}

	public async Task<string> GetAsync(string endpoint, Dictionary<string, string>? headers = null)
	{
		try
		{
			HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, endpoint);
			if (headers != null)
			{
				foreach (KeyValuePair<string, string> header in headers)
				{
					httpRequestMessage.Headers.Add(header.Key, header.Value);
				}
			}
			using HttpResponseMessage response = await _httpClient.SendAsync(httpRequestMessage);
			string text = await response.Content.ReadAsStringAsync();
			if (response.StatusCode == HttpStatusCode.Unauthorized)
			{
				Log.Warning("Warning: Your subscription time has expired, or the AccessToken has expired, try refreshing.", Array.Empty<object>());
			}
			if (!response.IsSuccessStatusCode)
			{
				throw new HttpRequestException($"Request failed with status code {response.StatusCode}. Response: {text}");
			}
			return text;
		}
		catch (HttpRequestException)
		{
			throw;
		}
		catch (Exception ex2)
		{
			Exception ex3 = ex2;
			throw new Exception("Error processing API response: " + ex3.Message);
		}
	}

	private async Task<byte[]> DownloadFileAsync(string endpoint, string id, Dictionary<string, string>? headers = null)
	{
		HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, endpoint);
		if (headers != null)
		{
			foreach (KeyValuePair<string, string> header in headers)
			{
				httpRequestMessage.Headers.Add(header.Key, header.Value);
			}
		}
		try
		{
			using HttpResponseMessage response = await _httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead);
			if (response.StatusCode == HttpStatusCode.Unauthorized)
			{
				Log.Warning("Warning: Your subscription time has expired,\nor the AccessToken has expired, try refreshing.", Array.Empty<object>());
			}
			if (!response.IsSuccessStatusCode)
			{
				throw new HttpRequestException($"Request failed with status code {response.StatusCode}. Response: {response.Content}");
			}
			long totalBytes = response.Content.Headers.ContentLength ?? (-1);
			byte[] result;
			await using (Stream contentStream = await response.Content.ReadAsStreamAsync())
			{
				using MemoryStream memoryStream = new MemoryStream();
				byte[] buffer = new byte[8192];
				long totalBytesRead = 0L;
				while (true)
				{
					int num2;
					int num = (num2 = await contentStream.ReadAsync(buffer));
					int bytesRead = num2;
					if (num <= 0)
					{
						break;
					}
					await memoryStream.WriteAsync(buffer.AsMemory(0, bytesRead));
					totalBytesRead += bytesRead;
					if (totalBytes > 0)
					{
						int arg = (int)(totalBytesRead * 100 / totalBytes);
						_progressCallback(id, arg);
					}
				}
				if (totalBytes <= 0)
				{
					_progressCallback(id, 100);
				}
				result = memoryStream.ToArray();
			}
			return result;
		}
		catch (HttpRequestException ex)
		{
			throw new Exception("Error processing API response: " + ex.Message);
		}
	}

	private void Dispose(bool disposing)
	{
		if (!_disposed)
		{
			if (disposing)
			{
				_httpClient.Dispose();
			}
			_disposed = true;
		}
	}

	~WebNexusApi()
	{
		Dispose(disposing: false);
	}
}
