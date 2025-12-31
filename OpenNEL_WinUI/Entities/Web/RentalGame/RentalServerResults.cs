/*
<OpenNEL>
Copyright (C) <2025>  <OpenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/
using System.Collections.Generic;
using System.ComponentModel;
using Codexus.Development.SDK.Entities;

namespace OpenNEL_WinUI.Entities.Web.RentalGame;

public class RentalServerItem : INotifyPropertyChanged
{
    private string _entityId = string.Empty;
    private string _name = string.Empty;
    private int _playerCount;
    private bool _hasPassword;
    private string _mcVersion = string.Empty;

    public string EntityId { get => _entityId; set { _entityId = value; OnPropertyChanged(nameof(EntityId)); } }
    public string Name { get => _name; set { _name = value; OnPropertyChanged(nameof(Name)); } }
    public int PlayerCount { get => _playerCount; set { _playerCount = value; OnPropertyChanged(nameof(PlayerCount)); } }
    public bool HasPassword { get => _hasPassword; set { _hasPassword = value; OnPropertyChanged(nameof(HasPassword)); } }
    public string McVersion { get => _mcVersion; set { _mcVersion = value; OnPropertyChanged(nameof(McVersion)); } }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class ListRentalServersResult
{
    public bool NotLogin { get; set; }
    public bool Success { get; set; }
    public string? Message { get; set; }
    public List<RentalServerItem> Items { get; set; } = new();
    public bool HasMore { get; set; }
}

public class RentalRoleItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class RentalServerRolesResult
{
    public bool NotLogin { get; set; }
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string ServerId { get; set; } = string.Empty;
    public List<RentalRoleItem> Items { get; set; } = new();
}

public class JoinRentalGameResult
{
    public bool Success { get; set; }
    public bool NotLogin { get; set; }
    public string? Message { get; set; }
    public string Ip { get; set; } = string.Empty;
    public int Port { get; set; }
}

public class EntityJoinRentalGame
{
    public string ServerId { get; set; } = string.Empty;
    public string ServerName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string GameId { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string McVersion { get; set; } = string.Empty;
    public EntitySocks5? Socks5 { get; set; }
}
