using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SkyFrost.Base;

namespace Restrainite.Enums;

public enum WorldPermissionType
{
    AnyoneHidden,
    AnyoneNonHidden,
    RegisteredUsersHidden,
    RegisteredUsersNonHidden,
    ContactsPlusHidden,
    ContactsPlusNonHidden,
    ContactsHidden,
    ContactsNonHidden,
    LanHidden,
    LanNonHidden,
    PrivateHidden,
    PrivateNonHidden
}

internal static class WorldPermissionTypes
{
    internal static readonly IEnumerable<WorldPermissionType> List =
        Enum.GetValues(typeof(WorldPermissionType)).Cast<WorldPermissionType>();

    private static readonly Dictionary<WorldPermissionType, string> Dictionary =
        List.ToDictionary(l => l,
            l => Regex.Replace(l.ToString(), "([a-z])([A-Z])", "$1 $2"));

    internal static string AsExpandedString(this WorldPermissionType type)
    {
        return Dictionary[type];
    }

    internal static PresetChangeType Default(this WorldPermissionType type)
    {
        return type switch
        {
            WorldPermissionType.AnyoneHidden or WorldPermissionType.AnyoneNonHidden
                or WorldPermissionType.RegisteredUsersHidden
                or WorldPermissionType.RegisteredUsersNonHidden or WorldPermissionType.ContactsPlusHidden
                or WorldPermissionType.ContactsPlusNonHidden or WorldPermissionType.ContactsNonHidden
                or WorldPermissionType.LanNonHidden => PresetChangeType.None,
            WorldPermissionType.LanHidden or WorldPermissionType.ContactsHidden or WorldPermissionType.PrivateHidden
                or WorldPermissionType.PrivateNonHidden => PresetChangeType.DoNotChange,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    internal static WorldPermissionType FromResonite(SessionAccessLevel sessionAccessLevel, bool hidden)
    {
        return sessionAccessLevel switch
        {
            SessionAccessLevel.Private => hidden
                ? WorldPermissionType.PrivateHidden
                : WorldPermissionType.PrivateNonHidden,
            SessionAccessLevel.LAN => hidden
                ? WorldPermissionType.LanHidden
                : WorldPermissionType.LanNonHidden,
            SessionAccessLevel.Contacts => hidden
                ? WorldPermissionType.ContactsHidden
                : WorldPermissionType.ContactsNonHidden,
            SessionAccessLevel.ContactsPlus => hidden
                ? WorldPermissionType.ContactsPlusHidden
                : WorldPermissionType.ContactsPlusNonHidden,
            SessionAccessLevel.RegisteredUsers => hidden
                ? WorldPermissionType.RegisteredUsersHidden
                : WorldPermissionType.RegisteredUsersNonHidden,
            SessionAccessLevel.Anyone =>
                hidden ? WorldPermissionType.AnyoneHidden : WorldPermissionType.AnyoneNonHidden,
            _ => throw new ArgumentOutOfRangeException(nameof(sessionAccessLevel), sessionAccessLevel, null)
        };
    }
}