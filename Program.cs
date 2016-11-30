using System;
using System.Linq;
using System.Collections.Generic;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using SharpDX;

namespace ParaSyndra
{
	class Program
	{
		static Menu Config;
		
		static readonly Dictionary<int, GameObject> GrabableW = new Dictionary<int, GameObject>();
		
		static readonly Spell.Skillshot Q = new Spell.Skillshot(SpellSlot.Q, 800, EloBuddy.SDK.Enumerations.SkillShotType.Circular, 250, int.MaxValue, 250, DamageType.Magical) { MinimumHitChance = EloBuddy.SDK.Enumerations.HitChance.High };
		
		static readonly Spell.Skillshot W = new Spell.Skillshot(SpellSlot.W, 950, EloBuddy.SDK.Enumerations.SkillShotType.Circular, 250, 2500, 250, DamageType.Magical) { MinimumHitChance = EloBuddy.SDK.Enumerations.HitChance.High };

		static readonly Spell.Skillshot E = new Spell.Skillshot(SpellSlot.E, 700, EloBuddy.SDK.Enumerations.SkillShotType.Linear, 250, 2500, 55, DamageType.Magical) { MinimumHitChance = EloBuddy.SDK.Enumerations.HitChance.High };

		static readonly Spell.Targeted R = new Spell.Targeted(SpellSlot.R, 675, DamageType.Magical);
		
		static readonly Spell.Targeted R5 = new Spell.Targeted(SpellSlot.R, 750, DamageType.Magical);
				
		public static void Main(string[] args)
		{
			Loading.OnLoadingComplete += Loading_OnLoadingComplete;
		}

		static void Loading_OnLoadingComplete(EventArgs args)
		{
			Config = MainMenu.AddMenu("ParaSyndra", "parasyndra");
			Config.AddGroupLabel("Ulti ON:");
			foreach (var enemy in EntityManager.Heroes.Enemies)
			{
				Config.Add(enemy.ChampionName, new CheckBox(enemy.ChampionName));
			}
			Config.AddSeparator();
			Config.AddGroupLabel("AUTO Harras:");
			Config.Add("qh", new CheckBox("Q"));
			Config.Add("wh", new CheckBox("W", false));
			Config.Add("qeh", new CheckBox("E STUN ONLY", false));
			Config.Add("mm", new Slider("Minimum Mana Percent", 75));
			Game.OnUpdate += Game_OnUpdate;
			GameObject.OnCreate += GameObject_OnCreate;
			GameObject.OnDelete += GameObject_OnDelete;
			Obj_AI_Base.OnSpellCast += Obj_AI_Base_OnSpellCast;
		}
		
		static bool cannextw = true;
		
		static float nextw;

		static void Game_OnUpdate(EventArgs args)
		{
			if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
			{
				RLogic();
				WLogic();
				QLogic();
				ELogic();
			}
			else if ((Player.Instance.Mana / Player.Instance.MaxMana) * 100 > Config["mm"].Cast<Slider>().CurrentValue)
			{
				if (Config["qh"].Cast<CheckBox>().CurrentValue)
				{
					QLogic();
				}
				if (Config["qeh"].Cast<CheckBox>().CurrentValue)
				{
					ELogic();
				}
				if (Config["wh"].Cast<CheckBox>().CurrentValue)
				{
					WLogic();
				}
			}
		}
		
		static void QLogic()
		{
			if (Q.IsReady())
			{
				var target = TargetSelector.GetTarget(1000, DamageType.Magical);
				if (target.IsValidTarget())
				{
					if (Q.GetPrediction(target).CastPosition.Distance(Player.Instance) < 800)
					{
						Q.Cast(target);
					}
					else
					{
						var t = TargetSelector.GetTarget(800, DamageType.Magical);
						if (t.IsValidTarget())
						{
							Q.Cast(t);
						}
					}
				}
			}
		}
		
		static Vector3 wcastpos;
		
		static void WLogic()
		{
			if (Game.Time * 1000 > nextw + 3000)
			{
				wcastpos = Vector3.Zero;
				cannextw = true;
			}
			if (!wcastpos.IsZero && Player.Instance.HasBuff("syndrawtooltip"))
			{
				Player.CastSpell(SpellSlot.W, wcastpos);
			}
			else if (wcastpos.IsZero && Player.Instance.HasBuff("syndrawtooltip"))
			{
				cannextw = false;
				nextw = Game.Time * 1000;
				var t = TargetSelector.GetTarget(950, DamageType.Magical);
				if (t.IsValidTarget())
				{
					W.Cast(t);
				}
			}
			else if (cannextw && W.IsReady())
			{
				Vector3 pos = Vector3.Zero;
				foreach (var syndrasq in GrabableW.Where(x=>x.Value.Position.Distance(Player.Instance)<900))
				{
					pos = syndrasq.Value.Position;
					break;
				}
				if (pos.IsZero)
				{
					foreach (var minion in EntityManager.MinionsAndMonsters.EnemyMinions.Where(x=>x.Position.Distance(Player.Instance)<900))
					{
						pos = minion.Position;
						break;
					}
				}
				if (!pos.IsZero)
				{
					var target = TargetSelector.GetTarget(1050, DamageType.Magical);
					if (target.IsValidTarget())
					{
						Vector3 p1 = W.GetPrediction(target).CastPosition;
						if (p1.Distance(Player.Instance) < 900)
						{
							wcastpos = p1;
							if (pos.Distance(Player.Instance) < 925)
							{
								Player.CastSpell(SpellSlot.W, pos);
								cannextw = false;
								nextw = Game.Time * 1000;
								return;
							}
						}
						else
						{
							var t = TargetSelector.GetTarget(950, DamageType.Magical);
							if (t.IsValidTarget())
							{
								Vector3 p2 = W.GetPrediction(t).CastPosition;
								if (p2.Distance(Player.Instance) < 900)
								{
									wcastpos = p2;
									if (pos.Distance(Player.Instance) < 925)
									{
										Player.CastSpell(SpellSlot.W, pos);
										cannextw = false;
										nextw = Game.Time * 1000;
									}
								}
							}
						}
					}
				}
			}
		}
		
		static void ELogic()
		{
			if (E.IsReady() && Game.Time * 1000 > nextw + 500)
			{
				var target = TargetSelector.GetTarget(1100, DamageType.Magical);
				if (target.IsValidTarget())
				{
					foreach (var syndrasq in GrabableW.Where(x=>x.Value.Position.Distance(Player.Instance)<700 && x.Value.Position.Distance(Player.Instance) < target.Distance(Player.Instance) && x.Value.Position.Distance(target) < target.Distance(Player.Instance)))
					{
						Vector3 pos = syndrasq.Value.Position;
						Vector3 cast = E.GetPrediction(target).CastPosition;
						float dist = DistanceFromPointToLine(cast, Player.Instance.Position, pos);
						if (dist < 35f + target.BoundingRadius)
						{
							Player.CastSpell(SpellSlot.E, cast);
							break;
						}
					}
				}
			}
		}
		
		static void RLogic()
		{
			if (R.IsReady())
			{
				float extra = 0f;
				int level = R.Level;
				if (level == 3)
				{
					extra = level * 25;
				}
				var target = TargetSelector.GetTarget(675f + extra, DamageType.Magical);
				if (target.IsValidTarget() && CanUlt(target) && Config[target.ChampionName].Cast<CheckBox>().CurrentValue)
				{
					R.Cast(target);
				}
			}
		}

		static void GameObject_OnCreate(GameObject sender, EventArgs args)
		{
			if (sender.Name == "Syndra_Base_Q_idle.troy" || sender.Name == "Syndra_Base_Q_Lv5_idle.troy")
			{
				int id = sender.NetworkId;
				GrabableW.Add(id, sender);
			}
		}

		static void GameObject_OnDelete(GameObject sender, EventArgs args)
		{
			int id = sender.NetworkId;
			if (GrabableW.ContainsKey(id))
			{
				GrabableW.Remove(id);
			}
		}

		static void Obj_AI_Base_OnSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
		{
			if (sender.IsMe && args.Slot == SpellSlot.W)
			{
				cannextw = false;
				nextw = Game.Time * 1000;
			}
		}
		
		static bool CanUlt(AIHeroClient unit)
		{
			float magicresist = (unit.SpellBlock - Player.Instance.FlatMagicPenetrationMod) * Player.Instance.PercentMagicPenetrationMod;
			float damage = (1f - (magicresist / (magicresist + 100))) * (3 + GrabableW.Count()) * (new[] { 90, 135, 180 }[R.Level - 1] + (Player.Instance.TotalMagicalDamage * 0.2f));
			if (damage + 250f > unit.MagicShield + unit.Health && unit.Health / unit.MaxHealth * 100 > 10)
			{
				return true;
			}
			return false;
		}
		
		public static float DistanceFromPointToLine(Vector3 point, Vector3 l1, Vector3 l2)
		{
			return (float)(Math.Abs((l2.X - l1.X) * (l1.Y - point.Y) - (l1.X - point.X) * (l2.Y - l1.Y)) / Math.Sqrt(Math.Pow(l2.X - l1.X, 2) + Math.Pow(l2.Y - l1.Y, 2)));
		}
	}
}
