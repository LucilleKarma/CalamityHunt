﻿using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.GameContent;
using ReLogic.Content;
using Terraria.Graphics.Shaders;

namespace CalamityHunt.Content.Projectiles.Weapons
{
	public class GoombombShard : ModProjectile
	{
		public override string Texture => "CalamityHunt/Content/Items/Weapons/Goozmaga";

		const float maxStacks = 10;

		public override void SetDefaults()
		{
			Projectile.width = 40;
			Projectile.height = 40;
			Projectile.friendly = true;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 720;
			Projectile.tileCollide = false;
			Projectile.DamageType = DamageClass.Throwing;
			if (ModLoader.HasMod("CalamityMod"))
			{
				ModRarity r;
				DamageClass d;
				Mod calamity = ModLoader.GetMod("CalamityMod");
				calamity.TryFind<DamageClass>("RogueDamageClass", out d);
				Projectile.DamageType = d;
			}
		}

		public override void AI()
		{
			if (Projectile.localAI[1] == 0)
            {
				SoundEngine.PlaySound(SoundID.DD2_EtherianPortalOpen, Projectile.Center);
				Projectile.localAI[1] = 1;
            }
			bool hasTarget = Projectile.ai[0] > -1;
			NPC target = hasTarget ? Main.npc[(int)Projectile.ai[0]] : null;
			if (target != null && target.chaseable && target.active)
			{
				double deg = Main.GlobalTimeWrappedHourly * 360 + 120;
				double rad = deg * (Math.PI / 180);
				float hyposx = target.Center.X - (int)(Math.Cos(rad) * 100) - Projectile.width / 2;
				float hyposy = target.Center.Y - (int)(Math.Sin(rad) * 100) - Projectile.height / 2;

				Projectile.position = new Vector2(hyposx, hyposy);

			}
			if (Projectile.localAI[0] > 0)
            {
				Projectile.localAI[0]--;
            }
			if (Projectile.ai[0] > -1 && !target.active)
            {
				Projectile.Kill();
            }
		}

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
			bool projCount = false;
			bool amIOrbitting = Projectile.ai[0] > -1 ? true : false;
			if (!amIOrbitting)
			{
				for (int i = 0; i < Main.maxProjectiles; i++)
				{
					Projectile proj = Main.projectile[i];
					if (proj.type == ModContent.ProjectileType<GoombombShard>() && proj.whoAmI != Projectile.whoAmI && proj.ai[0] == target.whoAmI && proj.active)
					{
						projCount = true;
						break;
					}
				}
				if (projCount)
				{
					for (int i = 0; i < Main.maxProjectiles; i++)
					{
						Projectile proj = Main.projectile[i];
						if (proj.type == ModContent.ProjectileType<GoombombShard>() && proj.whoAmI != Projectile.whoAmI && proj.ai[0] == target.whoAmI && proj.active)
						{
							SoundEngine.PlaySound(SoundID.Item28 with { Volume = SoundID.Item28.Volume * 0.8f }, Projectile.Center);
							proj.localAI[0] += 60;
							if (proj.timeLeft < maxStacks * 60)
							{
								proj.timeLeft += 60;
							}
							Projectile.Kill();
							break;
						}
					}
				}
				if (target.active && !projCount && Projectile.ai[0] == -1)
				{
					SoundEngine.PlaySound(SoundID.Item62, Projectile.position);
					Projectile.ai[0] = target.whoAmI;
					if (Projectile.ai[1] == 1)
					{
						SoundEngine.PlaySound(SoundID.Item89, Projectile.position);
						for (int i = 0; i < 2; i++)
							Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, new Vector2(0, -20), ModContent.ProjectileType<GoombombProj>(), (int)Projectile.damage / 2, Projectile.knockBack, Main.myPlayer, Projectile.whoAmI, i + 1);
					}
				}
			}
        }
    }
}
