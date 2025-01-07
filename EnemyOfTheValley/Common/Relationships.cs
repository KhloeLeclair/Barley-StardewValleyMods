﻿using StardewValley;
using static StardewValley.Menus.SocialPage;

namespace EnemyOfTheValley.Common
{
    internal class Relationships
    {
        public static FriendshipStatus Enemy = (FriendshipStatus)(-1);
        public static FriendshipStatus Archenemy = (FriendshipStatus)(-2);
        public static FriendshipStatus ExArchenemy = (FriendshipStatus)(-3);

        public static void SetRelationship(string name, FriendshipStatus status)
        {
            Game1.player.friendshipData[name].Status = status;
        }

        public static bool IsRelationship(SocialEntry entry, FriendshipStatus status)
        {
            if (entry is null || entry.Friendship is null) return false;
            return IsRelationship(entry.Friendship, status);
        }

        public static bool IsRelationship(Friendship friendship, FriendshipStatus status)
        {
            return friendship.Status == status;
        }
    }
}
