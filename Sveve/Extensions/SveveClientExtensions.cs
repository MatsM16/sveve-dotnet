using System;
using System.Collections.Generic;

namespace Sveve.Extensions;

/// <summary>
/// Extensions for the SveveClient.
/// </summary>
internal static class SveveClientExtensions
{
    internal static SveveCommandBuilder Command(this SveveClient client, string endpoint, string command)
    {
        return new SveveCommandBuilder(client, endpoint, command);
    }
}
