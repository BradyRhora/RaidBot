using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace RaidBot
{
    public partial class Raid
    {
        public class Player : Profile
        {
            public Player(IUser user, Game game) : base(user) => Initialize(game);
            public Player(Profile user, Game game) : base(user.DiscordUser) => Initialize(game);

            void Initialize(Game game)
            {
                MaxHealth = Power * 10;
                if (MaxHealth < 1) MaxHealth = 1;
                Health = MaxHealth;
                Game = game;
            }

            string Attack(Placeable target, int damage)
            {
                var resultMSG = target.TakeDamage(damage);
                string xtraMSG = "";
                if (resultMSG != null)
                {
                    xtraMSG = "\n" + resultMSG;
                    if (target.GetType() == typeof(Monster))
                    {
                        int exp = ((Monster)target).GetDeathEXP();
                        string xtraLvlMsg = GiveEXP(exp);
                        xtraMSG += " You gained " + exp + " experience.";
                        if (xtraLvlMsg != "") xtraMSG += '\n' + xtraLvlMsg;
                    }
                }
                else if (target.Health <= (target.MaxHealth / 2)) xtraMSG = " It looks pretty hurt!";
                return xtraMSG;
            }

            void PickupItem(Item item)
            {
                var inv = Inventory.ToList();
                inv.Add(item);
                Inventory = inv.ToArray();
            }

            string Move(Direction dir, int steps = 1)
            {
                string itemMsg = "";
                bool moveFailed = false;
                for (int i = 0; i < steps; i++)
                {
                    switch (dir)
                    {
                        case Direction.Left:
                            if (Game.GetCurrentRoom().IsSpaceEmpty(X - 1, Y, includeDead: false))
                                X--;
                            else
                                moveFailed = true;

                            break;
                        case Direction.Right:
                            if (Game.GetCurrentRoom().IsSpaceEmpty(X + 1, Y, includeDead: false))
                                X++;
                            else
                                moveFailed = true;
                            break;
                        case Direction.Down:
                            if (Game.GetCurrentRoom().IsSpaceEmpty(X, Y + 1, includeDead: false))
                                Y++;
                            else
                                moveFailed = true;
                            break;
                        case Direction.Up:
                            if (Game.GetCurrentRoom().IsSpaceEmpty(X, Y - 1, includeDead: false))
                                Y--;
                            else
                                moveFailed = true;
                            break;
                        default:
                            moveFailed = true;
                            break;
                    }

                    if (moveFailed)
                    {
                        return "You are stopped by something in your way.\n" + Game.ShowCurrentRoom(describe: false);
                    }

                    var item = Game.GetCurrentRoom().GetPlaceableAt(X, Y, types: typeof(Item));

                    if (item != null)
                    {
                        PickupItem((Item)item);
                        Game.GetCurrentRoom().Loot.Remove((Item)item);
                        itemMsg = $"You picked up a(n) {item.GetName()} {item.GetEmote()}.\n";
                    }
                    StepsLeft--;
                    Moved = true;
                }
                return itemMsg + Game.ShowCurrentRoom(describe: false);
            }
            public string Act(string[] commands)
            {
                string actionS = commands[0];
                Action action = Action.GetActionByName(actionS);
                Direction dir = Direction.None;
                var currentRoom = Game.GetCurrentRoom();
                if (commands.Count() > 1)
                {
                    string direction = commands[1];
                    switch (direction)
                    {
                        case "l":
                        case "left":
                            dir = Direction.Left;
                            break;
                        case "r":
                        case "right":
                            dir = Direction.Right;
                            break;
                        case "u":
                        case "up":
                            dir = Direction.Up;
                            break;
                        case "d":
                        case "down":
                            dir = Direction.Down;
                            break;
                    }
                }

                if (action == null) return null;
                if (action.Equals(Action.Move))
                {
                    int steps = 1;
                    if (commands.Count() > 2)
                        steps = Convert.ToInt32(commands[2]);

                    if (steps < 0 || steps > StepsLeft) return null;

                    return Move(dir, steps);
                }
                else if (action == Action.Attack)
                {
                    int[] attackCoords = new int[] { X, Y };
                    switch (dir)
                    {
                        case Direction.Left:
                            attackCoords[0]--;
                            break;
                        case Direction.Right:
                            attackCoords[0]++;
                            break;
                        case Direction.Down:
                            attackCoords[1]++;
                            break;
                        case Direction.Up:
                            attackCoords[1]--;
                            break;
                        default:
                            return null;
                    }
                    Placeable target = Game.GetCurrentRoom().GetPlaceableAt(attackCoords[0], attackCoords[1], true, typeof(Monster), typeof(Player));
                    Acted = true;
                    Game.GetCurrentRoom().NextInitiative();
                    if (target == null) return $"{GetEmote()} {GetName()} attacks the air to their {dir.ToString().ToLower()}.";
                    else
                    {
                        var damage = RollAttackDamage();
                        var deathMSG = Attack(target, damage);

                        var weapon = Equipped;
                        string weaponName, weaponEmote;
                        if (weapon == null)
                        {
                            weaponName = "Fists";
                            weaponEmote = "✊";
                        }
                        else
                        {
                            weaponName = weapon.Name;
                            weaponEmote = weapon.Emote;
                        }
                        var equippedDmg = 0;
                        if (Equipped != null) equippedDmg = Equipped.Strength;
                        return $"{GetName()} attacks {target.GetName()} for (*roll: {damage - (Power / 2) - equippedDmg}* + {Power / 2 + equippedDmg}) = **{damage}** damage using their {weaponEmote} {weaponName}." + deathMSG;
                    }
                }
                else if (action == Action.Equip)
                {
                    if (commands.Count() < 2) return "You must specify an item in your inventory to equip.";
                    string itemStr = commands[1];
                    Item item = GetInventoryItemByName(itemStr);
                    if (item != null)
                    {
                        Equipped = item;
                        return $"{GetName()} prepares their {item.GetEmote()} {item.GetName()} for combat.";
                    }
                    else return $"Item not found. Are you sure you have a(n) '{itemStr}'?";
                }
                else if (action == Action.Pass)
                {
                    Acted = true;
                }
                else if (action.GetType() == typeof(Spell) || action.GetType() == typeof(Skill))
                {
                    if (!Actions.Contains(action))
                        return "You cannot use this action.";

                    var details = action.UseAction();

                    if (action.RequiresDirection && DirectionEquals(dir, details.PossibleDirections)) //checks if spell requires direction, and if the inputted direction is correct
                    {

                        int dirX = 0, dirY = 0;
                        switch (dir)
                        {
                            case Direction.Left:
                                dirX = -1;
                                break;
                            case Direction.Right:
                                dirX = 1;
                                break;
                            case Direction.Down:
                                dirY = 1;
                                break;
                            case Direction.Up:
                                dirY = -1;
                                break;
                            case Direction.UpLeft:
                                dirX = -1;
                                dirY = -1;
                                break;
                            case Direction.UpRight:
                                dirX = 1;
                                dirY = -1;
                                break;
                            case Direction.DownLeft:
                                dirX = -1;
                                dirY = 1;
                                break;
                            case Direction.DownRight:
                                dirX = 1;
                                dirY = 1;
                                break;
                        }
                        var continuous = false;
                        if (details.Effect != null) continuous = details.Effect.Continuous;
                        var contactCoords = Game.GetCurrentRoom().GetProjectileContact(X, Y, dirX, dirY, details.Range, returnAllSpaces: continuous, typeof(Monster), typeof(Player));
                        var target = Game.GetCurrentRoom().GetPlaceableAt(contactCoords[contactCoords.Length - 2], contactCoords[contactCoords.Length - 1]);

                        if (details.Effect != null)
                        {
                            if (continuous)
                            {
                                for (int i = 0; i < contactCoords.Count(); i += 2)
                                {
                                    int x = contactCoords[i];
                                    int y = contactCoords[i + 1];

                                    currentRoom.AddToInitative(details.Effect.CreateInstance(x, y), currentRoom.Counter + 1);
                                }
                            }
                            else if (details.Effect.Monster != null)
                            {
                                currentRoom.AddToInitative(details.Effect.CreateInstance(contactCoords[0], contactCoords[1], Game, owner: this), currentRoom.Initiative.ElementAt(currentRoom.Counter).Value + 1);
                            }
                        }

                        Acted = true;
                        currentRoom.NextInitiative();
                        if (target == null) return $"{GetEmote()} {GetName()} {action.ActTerm}s {action.Name} to the {dir.ToString().ToLower()}.";
                        else
                        {
                            double spellPower = 1;
                            bool negative = false;
                            if (details.Power < 0)
                            {
                                spellPower = details.Power * -1;
                                negative = true;
                            }
                            else spellPower = details.Power;
                            int roll = rdm.Next((int)(Magic_Power * spellPower));
                            var damage = roll + Magic_Power / 2;
                            if (negative) damage *= -1;
                            var deathMSG = Attack(target, damage);
                            string dmgWord = "damage";
                            if (negative) dmgWord = "health";
                            return $"{GetName()} uses {action.EffectEmote} {action.Name} on {target.GetName()} for **{damage}** {dmgWord}." + deathMSG;
                        }

                    }
                    else return "direction WRONK";
                }

                if (Acted) currentRoom.NextInitiative();
                return "nomsg";
            }
        }

    }
}
