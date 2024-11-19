using System;
using System.Linq;
using CalamityHunt.Common.Systems.Particles;
using CalamityHunt.Content.NPCs.Bosses.GoozmaBoss.Projectiles;
using CalamityHunt.Content.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;
using Terraria.ModLoader;
using static CalamityHunt.Common.Systems.ConditionalValue;

namespace CalamityHunt.Content.NPCs.Bosses.GoozmaBoss
{
    public partial class Goozma : ModNPC
    {
        /// <summary>
        /// How long it takes for Goozma to begin attacking during his Ebonian Bubbles attack.
        /// </summary>
        public static int EbonianBubbles_AttackStartDelay => 50;

        /// <summary>
        /// How many jumps should be performed during Goozma's Ebonian Bubbles attack.
        /// </summary>
        public static int EbonianBubbles_JumpCount => (int)DifficultyBasedValue(3, 4, 5, 6, master: 5, masterrev: 6, masterdeath: 8);

        /// <summary>
        /// How long jumps last during Goozma's Ebonian Bubbles attack.
        /// </summary>
        public static int EbonianBubbles_JumpDuration => (int)DifficultyBasedValue(120, 100, 70, 60, master: 70, masterrev: 60, masterdeath: 55);

        /// <summary>
        /// Performs Goozma's Ebonian Bubbles attack, alongside an ebonian slime.
        /// </summary>
        public void DoBehavior_EbonianBubbles()
        {
            // Stay above the target, with mild predictiveness.
            AresLockTo(Target.Center - Vector2.UnitY * 400f + Target.Velocity * new Vector2(5f, 2f));

            // Puppeteer the ebonian slime.
            DoBehavior_EbonianBubbles_Puppeteer();

            if (Time > EbonianBubbles_AttackStartDelay) {
                // Lose a bit of the aforementioned speed, at least on the X axis.
                NPC.velocity.X *= 0.8f;

                if (!NPC.WithinRange(Target.Center, 300f)) {
                    if (Time % 15 == 0) {
                        SoundStyle fizzSound = AssetDirectory.Sounds.Goozma.SlimeShoot;
                        SoundEngine.PlaySound(fizzSound, NPC.Center);
                        goozmaShootPowerTarget = 1f;
                    }

                    // Release projectiles.
                    int slimeShotID = ModContent.ProjectileType<SlimeShot>();
                    float angleSpread = MathHelper.SmoothStep(1.5f, 0.9f, Time / 350f);
                    if (Main.netMode != NetmodeID.MultiplayerClient && Time % 6 == 0) {

                        Projectile.NewProjectileDirect(NPC.GetSource_FromAI(), NPC.Center, Vector2.UnitY.RotatedBy(angleSpread).RotatedByRandom(0.3f) * Main.rand.Next(4, 7), slimeShotID, GetDamage(1), 0);
                        Projectile.NewProjectileDirect(NPC.GetSource_FromAI(), NPC.Center, Vector2.UnitY.RotatedBy(-angleSpread).RotatedByRandom(0.3f) * Main.rand.Next(4, 7), slimeShotID, GetDamage(1), 0);
                    }
                    if (Main.netMode != NetmodeID.MultiplayerClient && (Time + 6) % 6 == 0) {

                        Projectile.NewProjectileDirect(NPC.GetSource_FromAI(), NPC.Center, Vector2.UnitY.RotatedBy(angleSpread + 0.3f).RotatedByRandom(0.3f) * Main.rand.Next(2, 4), slimeShotID, GetDamage(1), 0);
                        Projectile.NewProjectileDirect(NPC.GetSource_FromAI(), NPC.Center, Vector2.UnitY.RotatedBy(-angleSpread - 0.3f).RotatedByRandom(0.3f) * Main.rand.Next(2, 4), slimeShotID, GetDamage(1), 0);
                    }
                    if (Main.netMode != NetmodeID.MultiplayerClient && Target.Center.Y < NPC.Top.Y - 300) {

                        Projectile.NewProjectileDirect(NPC.GetSource_FromAI(), NPC.Center, NPC.DirectionTo(Target.Center).SafeNormalize(Vector2.Zero) * Main.rand.Next(2, 8), slimeShotID, GetDamage(1), 0);
                    }
                }
            }
        }

        /// <summary>
        /// Makes Goozma puppeteer his slime ebonian slime during his Ebonian Bubbles attack.
        /// </summary>
        public void DoBehavior_EbonianBubbles_Puppeteer()
        {
            int npcIndex = NPC.FindFirstNPC(ModContent.NPCType<EbonianBehemuck>());
            if (npcIndex == -1 || npcIndex >= Main.maxNPCs) {
                return;
            }

            NPC npc = Main.npc[npcIndex];
            if (npc.ModNPC is not EbonianBehemuck ebonianSlime) {
                return;
            }

            ref float time = ref ebonianSlime.Time;
            ref Vector2 squishFactor = ref ebonianSlime.squishFactor;
            ref Vector2 saveTarget = ref ebonianSlime.saveTarget;

            int attackStartDelay = (int)(EbonianBubbles_AttackStartDelay * 0.8f);
            int jumpCount = EbonianBubbles_JumpCount;
            int jumpTime = EbonianBubbles_JumpDuration;

            // Allow the ebonian slime to take damage.
            npc.dontTakeDamage = false;

            if (time < attackStartDelay) {
                float xSquish = MathF.Pow(Utils.GetLerpValue(2, attackStartDelay, time, true), 2) * 0.4f;
                float ySquish = MathF.Pow(Utils.GetLerpValue(2, attackStartDelay, time, true), 2) * -0.6f;
                squishFactor = new Vector2(1f + xSquish, 1f + ySquish);

                // Rush to the target moments before the slam begins, to have a good starting point on the motion.
                if (time == attackStartDelay - 2f) {
                    npc.velocity += new Vector2(npc.DirectionTo(saveTarget).SafeNormalize(Vector2.Zero).X * 10f, -10f);
                    npc.netUpdate = true;
                }
            }

            else if (time < attackStartDelay + jumpCount * jumpTime) {
                float localTime = (time - attackStartDelay) % jumpTime;

                // Save where the target destination will be as the attack progresses in advance.
                if (localTime < (int)(jumpTime * 0.2f)) {
                    saveTarget = Target.Center + new Vector2(Target.Velocity.X * 45f, 380f);
                }

                // Ascend and play sounds prior to the jump.
                if (localTime == 0) {
                    npc.velocity.Y -= 10f;
                    SoundStyle hop = AssetDirectory.Sounds.Goozma.SlimeJump;
                    SoundEngine.PlaySound(hop, npc.Center);
                    SoundEngine.PlaySound(SoundID.QueenSlime, npc.Center);
                }

                else if (localTime < (int)(jumpTime * 0.8f)) {
                    Vector2 midPoint = new((npc.Center.X + saveTarget.X) / 2f, npc.Center.Y);

                    // Jump from the starting point to the midpoint, then jump from the midpoint to the target.
                    Vector2 startToMidpoint = Vector2.Lerp(npc.Center, midPoint, Utils.GetLerpValue(0f, jumpTime * 0.3f, localTime, true));
                    Vector2 midpointToTarget = Vector2.Lerp(midPoint, saveTarget, Utils.GetLerpValue(jumpTime * 0.3f, jumpTime * 0.75f, localTime, true));
                    Vector2 jumpTarget = Vector2.Lerp(startToMidpoint, midpointToTarget, Utils.GetLerpValue(0f, jumpTime * 0.7f, localTime, true));

                    // Adhere to the above intended jump movement.
                    Vector2 movementDirection = npc.DirectionTo(jumpTarget).SafeNormalize(Vector2.Zero);
                    float idealMovementSpeed = npc.Distance(jumpTarget) * Utils.GetLerpValue(0, jumpTime * 0.7f, localTime, true) * 0.7f;
                    float movementSharpness = MathF.Pow(Utils.GetLerpValue(0, jumpTime * 0.8f, localTime, true), 2f);
                    npc.velocity = Vector2.Lerp(npc.velocity, movementDirection * idealMovementSpeed, movementSharpness);

                    npc.rotation = -npc.velocity.Y * Math.Sign(npc.velocity.X) * 0.005f;

                    float resquish = Utils.GetLerpValue(jumpTime * 0.4f, 0, localTime, true) + Utils.GetLerpValue(jumpTime * 0.3f, jumpTime * 0.6f, localTime, true);
                    squishFactor = new Vector2(1f - MathF.Pow(resquish, 2) * 0.5f, 1f + MathF.Pow(resquish, 2) * 0.5f);

                    npc.frameCounter++;
                }

                // Release bubbles.
                if (localTime >= (int)(jumpTime * 0.8f) && localTime < (int)(jumpTime * 0.95f)) {
                    npc.velocity *= 0.2f;
                    npc.rotation = 0;

                    for (int i = 0; i < Main.rand.Next(1, 2); i++) {
                        Vector2 velocity = Main.rand.NextVector2Circular(20, 15) + Vector2.UnitY * 15 + npc.DirectionTo(Target.Center).SafeNormalize(Vector2.Zero) * 10f;

                        // For balancing reasons, revent bubbles from shooting directly up.
                        // Any bubbles that would have spawned shooting upwards with next to no horizontal velocity are pushed away.
                        if (Math.Abs(velocity.X) < 6f && velocity.Y < 0) {
                            velocity.X = Math.Sign(velocity.X) * 6f;
                        }
                        Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Bottom, velocity, ModContent.ProjectileType<ToxicSludge>(), GetDamage(1), 0);
                    }

                    if (localTime % 2 == 0) {
                        Main.instance.CameraModifiers.Add(new PunchCameraModifier(saveTarget, Main.rand.NextVector2CircularEdge(3, 3), 5f, 10, 12));
                    }

                    // Make the squish effect gradually return to normal as the jump concludes.
                    float squishX = MathF.Sqrt(Utils.GetLerpValue(jumpTime, jumpTime * 0.8f, localTime, true)) * 0.6f;
                    float squishY = MathF.Sqrt(Utils.GetLerpValue(jumpTime, jumpTime * 0.8f, localTime, true)) * -0.5f;
                    squishFactor = new Vector2(1f + squishX, 1f + squishY);
                }

                if (localTime == (int)(jumpTime * 0.74f)) {
                    foreach (Player player in Main.player.Where(n => n.active && !n.dead && n.Distance(npc.Center) < 600)) {
                        player.velocity += player.DirectionFrom(npc.Bottom + Vector2.UnitY * 10) * 3;
                    }

                    SoundStyle slam = AssetDirectory.Sounds.GoozmaMinions.SlimeSlam;
                    SoundEngine.PlaySound(slam, npc.Center);

                    for (int i = 0; i < Main.rand.Next(14, 20); i++) {
                        Vector2 velocity = Main.rand.NextVector2Circular(8, 1) - Vector2.UnitY * Main.rand.NextFloat(7f, 12f);
                        Vector2 position = npc.Center + Main.rand.NextVector2Circular(1, 50) + new Vector2(velocity.X * 15f, 32f);
                        CalamityHunt.particles.Add(Particle.Create<EbonGelChunk>(particle => {
                            particle.position = position;
                            particle.velocity = velocity;
                            particle.scale = Main.rand.NextFloat(0.1f, 2.1f);
                            particle.color = Color.White;
                        }));
                    }
                }
            }

            if (time > EbonianBubbles_AttackStartDelay + jumpCount * jumpTime) {
                ebonianSlime.Reset();
            }
        }
    }
}
