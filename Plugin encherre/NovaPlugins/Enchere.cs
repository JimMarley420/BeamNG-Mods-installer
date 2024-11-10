using Life;
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Life.Network;

namespace NovaPlugins
{
    public class Enchere : Plugin
    {
        private List<Auction> auctions;
        private readonly string filePath = "auctions.json";

        public Enchere(IGameAPI api) : base(api)
        {
            auctions = new List<Auction>();
            LoadAuctions();
            RegisterCommands();
            Console.WriteLine("Plugin d'enchères NovaLife initialisé");
            Console.WriteLine("Développé par Jimmy le meilleur");
        }

        private void RegisterCommands()
        {
            API.RegisterCommand("startAuction", StartAuctionCommand);
            API.RegisterCommand("bidAuction", BidAuctionCommand);
            API.RegisterCommand("viewAuctions", ViewAuctionsCommand);
        }

        private void StartAuctionCommand(Player player, string[] args)
        {
            if (args.Length >= 2)
            {
                string itemName = args[0];
                if (double.TryParse(args[1], out double startingPrice))
                {
                    Func<string> getFullName = player.GetFullName;
                    throw new NotImplementedException();
                }
                else
                {
                    object value = player.SendMessage("Usage: /startAuction <itemName> <startingPrice>");
                }
            }
            else
            {
                object value = player.SendMessage("Usage: /startAuction <itemName> <startingPrice>");
            }
        }

        private void StartAuction(string itemName, double startingPrice, object name)
        {
            throw new NotImplementedException();
        }

        private void BidAuctionCommand(Player player, string[] args)
        {
            if (args.Length >= 2)
            {
                if (int.TryParse(args[0], out int auctionId) && double.TryParse(args[1], out double bidAmount))
                {
                    BidAuction(auctionId, bidAmount, player.Name);
                }
                else
                {
                    player.SendMessage("Usage: /bidAuction <auctionId> <bidAmount>");
                }
            }
            else
            {
                player.SendMessage("Usage: /bidAuction <auctionId> <bidAmount>");
            }
        }

        private void ViewAuctionsCommand(Player player, string[] args)
        {
            ViewAuctions(player);
        }

        private void StartAuction(string itemName, double startingPrice, string owner)
        {
            Auction newAuction = new Auction(itemName, startingPrice, owner);
            auctions.Add(newAuction);
            Console.WriteLine($"Nouvelle enchère démarrée pour {itemName} avec un prix de départ de {startingPrice}");
        }

        private void BidAuction(int auctionId, double bidAmount, string bidder)
        {
            Auction auction = auctions.Find(a => a.Id == auctionId);
            if (auction != null && bidAmount > auction.CurrentBid)
            {
                auction.CurrentBid = bidAmount;
                auction.CurrentBidder = bidder;
                Console.WriteLine($"{bidder} a placé une enchère de {bidAmount} sur {auction.ItemName}");
            }
            else
            {
                Console.WriteLine("Enchère échouée : montant insuffisant ou enchère introuvable.");
            }
        }

        private void ViewAuctions(Player player)
        {
            foreach (var auction in auctions)
            {
                player.SendMessage($"Enchère ID: {auction.Id}, Item: {auction.ItemName}, Current Bid: {auction.CurrentBid}, Current Bidder: {auction.CurrentBidder}");
            }
        }

        private void SaveAuctions()
        {
            string json = JsonConvert.SerializeObject(auctions);
            File.WriteAllText(filePath, json);
        }

        private void LoadAuctions()
        {
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                auctions = JsonConvert.DeserializeObject<List<Auction>>(json);
            }
        }
    }

    public class Auction
    {
        private static int auctionCounter = 0;
        public int Id { get; }
        public string ItemName { get; }
        public double StartingPrice { get; }
        public double CurrentBid { get; set; }
        public string CurrentBidder { get; set; }
        public string Owner { get; }

        public Auction(string itemName, double startingPrice, string owner)
        {
            Id = auctionCounter++;
            ItemName = itemName;
            StartingPrice = startingPrice;
            CurrentBid = startingPrice;
            CurrentBidder = null;
            Owner = owner;
        }
    }
}
