﻿using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lotshared
{
    //The server sents each a client a Snapshot, representing the state of the world at a particular time.
    //The client interpolates between two snapshots to show smooth animation
    //TODO: make snapshots delta-encoded
    //Snapshots do not include Shots, because shots are fully client-simulated
    public class Snapshot
    {
        public long Timestamp;
        public long LastAckedClientMove;
        public Dictionary<int, Creature> Creatures = new Dictionary<int, Creature>();
        public Dictionary<long, Player> Players = new Dictionary<long, Player>();

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Packets.Snapshot);
            writer.Put(Timestamp);
            writer.Put(LastAckedClientMove);
            writer.Put(Creatures.Count);
            writer.Put(Players.Count);
            foreach (var c in Creatures.Values)
            {
                writer.Put(c.Id);
                writer.Put(c.X);
                writer.Put(c.Y);
                writer.Put((byte)c.Status);
            }
            foreach (var p in Players.Values)
            {
                writer.Put(p.Id);
                writer.Put(p.X);
                writer.Put(p.Y);
                writer.Put(p.Health);
                writer.Put(p.MaxHealth);
                writer.Put(p.Charge);
                writer.Put(p.MaxCharge);
            }
        }

        public static Snapshot Deserialize(NetDataReader reader)
        {
            Snapshot snapshot = new Snapshot();
            snapshot.Timestamp = reader.GetLong();
            snapshot.LastAckedClientMove = reader.GetLong();
            int cCount = reader.GetInt();
            int pCount = reader.GetInt();
            for (var i = 0; i < cCount; i++)
            {
                Creature c = new Creature();
                c.Id = reader.GetInt();
                c.X = reader.GetInt();
                c.Y = reader.GetInt();
                c.Status = (CreatureStatus)reader.GetByte();
                snapshot.Creatures.Add(c.Id, c);
            }
            for (var i = 0; i < pCount; i++)
            {
                Player p = new Player(reader.GetInt());
                p.X = reader.GetInt();
                p.Y = reader.GetInt();
                p.Health = reader.GetInt();
                p.MaxHealth = reader.GetInt();
                p.Charge = reader.GetInt();
                p.MaxCharge = reader.GetInt();
                snapshot.Players.Add(p.Id, p);
            }
            return snapshot;
        }
    }
}
