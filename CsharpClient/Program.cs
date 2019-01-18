using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CsharpClient
{
    public class Program
    {
        public static string Name { get; set; }

        public static string Tournament { get; set; }

        public static async Task Main(string[] args)
        {
            Name = "Name " + DateTime.Now.Ticks;
            Tournament = "Tournament #1";

            var connection = new HubConnectionBuilder()
                .WithUrl("http://localhost:64573/stocks")
                .ConfigureLogging(logging =>
                {
                    logging.AddConsole();
                })
                .AddMessagePackProtocol()
                .Build();

            await connection.StartAsync();

            Console.WriteLine("Starting connection. Press Ctrl-C to close.");
            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, a) =>
            {
                a.Cancel = true;
                cts.Cancel();
            };

            connection.Closed += e =>
            {
                Console.WriteLine("Connection closed with error: {0}", e);

                cts.Cancel();
                return Task.CompletedTask;
            };


            connection.On("marketOpened", () =>
            {
                Console.WriteLine("Market opened");
            });

            connection.On("marketClosed", () =>
            {
                Console.WriteLine("Market closed");
            });

            //connection.On("marketReset", () =>
            //{
            //    // We don't care if the market rest
            //});

            connection.On<string>(
                "Ack",
                consoleName =>
                    {
                        // We don't care if the market rest

                    Console.WriteLine("We got an Ack: " + consoleName);
                });


            connection.On<string, string>("JoinTournament", (name, tournament) => { Console.WriteLine($"{name} joined {tournament}"); });

            //connection.On<string, int, int, int, int>("RaceStateChange", (racer, x, y, verticalVelocity, horizontalVelocity) =>
            //    {
            //        var newState = new RaceState(x, y, verticalVelocity, horizontalVelocity);
            //        var next = raceBot.NextMove(racer, newState);

            //        connection.InvokeAsync("NextMove", racer, next);
            //    });

            //await connection.StartAsync(cts.Token);

            await connection.SendAsync("JoinTournament", Name, Tournament, cancellationToken: cts.Token);
            // await connection.SendAsync("Send", "Hello! Name joined Tournament", cancellationToken: cts.Token);
            
            var channel = await connection.StreamAsChannelAsync<Stock>("StreamStocks", CancellationToken.None);
            while (await channel.WaitToReadAsync(cts.Token) && !cts.IsCancellationRequested)
            {
                while (channel.TryRead(out var stock))
                {
                    Console.WriteLine($"{stock.Symbol} {stock.Price}");

                    if (stock.Symbol == "MSFT")
                    {
                        await connection.SendAsync("Ack", "Console", cancellationToken: cts.Token);
                    }
                }
            }
        }

    }

    public class Stock
    {
        public string Symbol { get; set; }

        public decimal DayOpen { get; private set; }

        public decimal DayLow { get; private set; }

        public decimal DayHigh { get; private set; }

        public decimal LastChange { get; private set; }

        public decimal Change { get; set; }

        public double PercentChange { get; set; }

        public decimal Price { get; set; }
    }
}
