using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CsharpClient
{
    public class Program
    {
        static async Task Main(string[] args)
        {
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

            connection.On("marketReset", () =>
            {
                // We don't care if the market rest
            });


            connection.On("Ack", (string s) =>
                {
                    // We don't care if the market rest

                    Console.WriteLine("We got an Ack: " + s);
                });

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
