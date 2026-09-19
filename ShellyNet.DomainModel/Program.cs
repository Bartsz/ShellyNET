using ShellyNET.Client;
using ShellyNET.Client.Model.Messages;
using System.Net;

namespace ShellyNet.DomainModel;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var client = new ShellyClient();
        var ct = CancellationToken.None;
        _ = ListenToStatusChanges(client, ct);
        _ = ListenToEventsAsync(client, ct);
        client.DisconnectedUnexpectedly += HandleDisconnected;
        client.UnsupportedMessageTypeReceived += HandleUnsupportedMessage;
        client.UnknownMessageReceived += HandleUnknownMessage;
        try
        {
            var ip = new IPAddress([192, 168, 1, 103]);
            await client.ConnectAsync(ip);
            var response = await client.Call_Shelly_GetDeviceInfoAsync();
            if (response.IsSuccess && response.Value is ShellyDeviceInfo info && info.AuthenticationEnabled == true)
            {
                await client.LoginToDeviceAsync("abcd");
            }
            var config = await client.Call_Shelly_GetDeviceConfigAsync();
            var status = await client.Call_Shelly_GetDeviceStatus();
            var updates = await client.Call_Shelly_CheckForUpdate();
           // await client.Call_Shelly_Reboot();

            while (true)
            {
                await Task.Delay(1150);
                //var status1 = await client.Call_Shelly_GetDeviceStatus();

            }


        }
        catch (Exception)
        {
            await client.DisconnectAsync();
        }

        await client.DisconnectAsync();


    }

    public static async Task ListenToStatusChanges(ShellyClient client, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var status = await client.GetShellyStatusMessageAsync(ct).ConfigureAwait(false);
            Console.WriteLine(
                $"""
                Id: {status.Id}
                Component: {status.ComponentType}
                Property: {status.PropertyName}
                Time" {status.Timestamp}
                Bool: {status.AsBool}
                Value: {status.Value.ToString()}
                """);
        }
    }

    public static async Task ListenToEventsAsync(ShellyClient client, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var status = await client.GetShellyEventAsync(ct).ConfigureAwait(false);
            Console.WriteLine(
                $"""
                Id: {status.Id}
                Component: {status.ComponentType}
                Property: {status.ComponentEvent}
                Time" {status.Timestamp}
                """);
        }

    }

    private static void HandleUnknownMessage(object? sender, ShellyUnknownMessageEventArgs e)
    {
        Console.WriteLine();
        Console.WriteLine("UnknownMessageRecieved:".PadLeft(10, '-').PadRight(10, '-'));
        Console.WriteLine(e.Payload);
        Console.WriteLine(e.Reason);
        Console.WriteLine("".PadLeft(30, '-'));
        Console.WriteLine();
    }

    private static void HandleUnsupportedMessage(object? sender, ShellyUnsupportedMessageTypeEventArgs e)
    {
        Console.WriteLine();
        Console.WriteLine("UnknownMessageRecieved:".PadLeft(10, '-').PadRight(10, '-'));
        Console.WriteLine(e.MessageLength);
        Console.WriteLine(e.MessageType.ToString());
        Console.WriteLine("".PadLeft(30, '-'));
        Console.WriteLine();
    }

    private static void HandleDisconnected(object? sender, ShellyUnexpectedDisconnectionEventArgs e)
    {
        Console.WriteLine("Disconnected");
    }



}
