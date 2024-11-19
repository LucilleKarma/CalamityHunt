using System;
using CalamityHunt.Common.Systems.Camera;
using CalamityHunt.Common.Systems.Particles;
using CalamityHunt.Common.Utilities;
using CalamityHunt.Content.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityHunt.Content.NPCs.Bosses.GoozmaBoss
{
    public partial class Goozma : ModNPC
    {
        /// <summary>
        /// How long Goozma spends exploding into splitting gel during his pahse transition split.
        /// </summary>
        public static int PhaseTransitionSplit_ExplodeTime => 50;

        /// <summary>
        /// How long Goozma spends waiting before he prepares ot reform during his phase transition split.
        /// </summary>
        public static int PhaseTransitionSplit_ReformDelay => 300;

        /// <summary>
        /// How long it takes for Goozma to reform during his phase transition split.
        /// </summary>
        public static int PhaseTransitionSplit_ReformTime => 270;

        /// <summary>
        /// How long Goozma spends regenerating his HP during his phase transition split.
        /// </summary>
        public static int PhaseTransitionSplit_HealthRegenerationTime => (int)(PhaseTransitionSplit_ReformTime * 0.8889f);

        /// <summary>
        /// How long Goozma's phase transition split lasts.
        /// </summary>
        public static int PhaseTransitionSplit_OverallDuration => PhaseTransitionSplit_ReformDelay + PhaseTransitionSplit_ReformTime;

        /// <summary>
        /// How long it takes for Goozma to give the player control of their camera again during his phase transition split.
        /// </summary>
        public static int PhaseTransitionSplit_CameraPanReturnTime => PhaseTransitionSplit_OverallDuration - 70;

        /// <summary>
        /// How much extra defense Goozma is granted after his phase 2 transition.
        /// </summary>
        public static int Phase2TransitionDefenseBoost => 20;

        /// <summary>
        /// Performs Goozma's phase transition split animation.
        /// </summary>
        public void DoBehavior_PhaseTransitionSplit()
        {
            // Immediately cease any and all motion as Goozma splits.
            NPC.velocity = Vector2.Zero;

            // Delete the active slime immediately. The fight will from this point onward be solo (Goozmites don't count).
            if (ActiveSlimeIndex > -1 && ActiveSlimeIndex <= Main.maxNPCs) {
                if (ActiveSlime.active) {
                    ActiveSlime.active = false;
                }
            }

            // Stay invincible and regenerate HP as the phase progresses.
            float hpRegenerationInterpolant = MathF.Pow(Utils.GetLerpValue(0f, PhaseTransitionSplit_HealthRegenerationTime, Time - PhaseTransitionSplit_ReformDelay, true), 3f);
            NPC.dontTakeDamage = true;
            NPC.life = 1 + (int)(hpRegenerationInterpolant * (NPC.lifeMax - 1f));

            // Make Goozma's eye size increase overall for the upcoming phase.
            float eyeGrowInterpolant = Utils.GetLerpValue(0f, PhaseTransitionSplit_ReformTime * 0.74f, Time - PhaseTransitionSplit_ReformDelay, true);
            eyePower = Vector2.SmoothStep(Vector2.One * 0.8f, Vector2.One * 0.9f, eyeGrowInterpolant);

            if (Time < 15) {
                KillSlime(currentSlime);
            }

            if (Main.netMode != NetmodeID.Server && Time > PhaseTransitionSplit_ExplodeTime - 5f && Time < PhaseTransitionSplit_ExplodeTime + 3f) {
                for (int i = 0; i < 5; i++) {
                    Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(10, 10), DustID.TintableDust, Main.rand.NextVector2CircularEdge(20f, 20f), 200, Color.Black, Main.rand.NextFloat(2, 4)).noGravity = true;
                    CalamityHunt.particles.Add(Particle.Create<ChromaticGelChunk>(particle => {
                        particle.position = NPC.Center + Main.rand.NextVector2Circular(10, 10);
                        particle.velocity = Main.rand.NextVector2Circular(20, 20) - Vector2.UnitY * 8f;
                        particle.scale = Main.rand.NextFloat(0.5f, 2f);
                        particle.color = Color.White;
                        particle.colorData = new ColorOffsetData(true, NPC.localAI[0]);
                    }));
                }
            }

            if (Time < PhaseTransitionSplit_ExplodeTime) {
                NPC.scale = (float)Math.Sqrt(Utils.GetLerpValue(PhaseTransitionSplit_ExplodeTime, PhaseTransitionSplit_ExplodeTime * 0.2f, Time, true));

                // Shake a bit.
                if (Main.netMode != NetmodeID.MultiplayerClient) {
                    NPC.Center += Main.rand.NextVector2Circular(5f, 5f);
                    NPC.netUpdate = true;
                }

                // Explode into particles.
                // These will reform back into Goozma later.
                if (Main.netMode != NetmodeID.Server) {
                    for (int i = 0; i < Main.rand.Next(1, 4); i++) {
                        CalamityHunt.particles.Add(Particle.Create<GoozmaGelBit>(particle => {
                            particle.position = NPC.Center + Main.rand.NextVector2Circular(30, 40);
                            particle.velocity = Main.rand.NextVector2CircularEdge(10f, 10f) + Main.rand.NextVector2Circular(20f, 20f);
                            particle.scale = Main.rand.NextFloat(1f, 2f);
                            particle.color = Color.White;
                            particle.colorData = new ColorOffsetData(true, (int)(PhaseTransitionSplit_ReformDelay - Time + Main.rand.Next(55)));
                            particle.holdTime = PhaseTransitionSplit_ReformDelay - PhaseTransitionSplit_ExplodeTime + 20;
                            particle.anchor = () => NPC.Center;
                        }));
                    }
                }

            }
            else {

                // Rematerialize, secretly increasing in scale as the gel particles overlay Goozma, to help sell the illusion that he's reforming.
                float remateriarlizeInterpolant = Utils.GetLerpValue(400, 500, Time, true);
                float idealScale = MathHelper.SmoothStep(0f, 1f, remateriarlizeInterpolant);
                NPC.scale = MathHelper.Lerp(NPC.scale, idealScale, 0.2f);
                soulScale = MathHelper.Lerp(soulScale, idealScale, 0.2f);
            }

            // Create goo splatters as Goozma explodes and reforms.
            bool exploding = Time < 70;
            bool reforming = Time > 400;
            if (Main.netMode != NetmodeID.Server && (exploding || reforming)) {
                CalamityHunt.particles.Add(Particle.Create<ChromaticGooBurst>(particle => {
                    particle.velocity = Main.rand.NextVector2Circular(2, 3);
                    particle.position = NPC.Center + particle.velocity * 27f * NPC.scale;
                    particle.scale = Main.rand.NextFloat(0.1f, 1.6f);
                    particle.color = Color.White;
                    particle.colorData = new ColorOffsetData(true, NPC.localAI[0] + Main.rand.NextFloat(0.2f, 0.5f));
                }));
            }

            // Pan the Camera towards goozma.
            DoBehavior_PhaseTransitionSplit_UpdateCamera();

            // Make Goozma's eye gleam as he reforms.
            if (Time >= PhaseTransitionSplit_ReformDelay * 0.96f && !initializedLocalP2Sparkle) {
                if (Main.netMode != NetmodeID.Server) {
                    CalamityHunt.particles.Add(Particle.Create<CrossSparkle>(particle => {
                        particle.velocity = MathHelper.PiOver4.ToRotationVector2();
                        particle.position = NPC.Center;
                        particle.scale = 2f;
                        particle.color = Color.White;
                    }));
                    CalamityHunt.particles.Add(Particle.Create<CrossSparkle>(particle => {
                        particle.velocity = Vector2.Zero;
                        particle.position = NPC.Center;
                        particle.scale = 4f;
                        particle.color = Color.White;
                    }));

                    SoundEngine.PlaySound(AssetDirectory.Sounds.Goozma.EyeAppear, NPC.Center);

                    SoundStyle reform = AssetDirectory.Sounds.Goozma.Reform;
                    SoundEngine.PlaySound(reform, NPC.Center);
                }
                initializedLocalP2Sparkle = true;
            }

            // Roar and make the music return for his second phase.
            if (Time >= PhaseTransitionSplit_OverallDuration && !initializedLocalP2Roar) {
                SoundStyle roar = AssetDirectory.Sounds.Goozma.Reawaken;
                SoundEngine.PlaySound(roar, NPC.Center);

                Music = Music2;
                Main.newMusic = Music2;

                try {
                    Main.musicFade[Main.curMusic] = 0f;
                    Main.musicFade[Main.newMusic] = 1f;
                }
                catch (IndexOutOfRangeException) {

                }

                NPC.dontTakeDamage = false;

                initializedLocalP2Roar = true;
            }

            // Transition to the next phase.
            if (Time > PhaseTransitionSplit_OverallDuration) {
                if (Main.netMode != NetmodeID.MultiplayerClient) {
                    PerformPhase2TransitionEffects();
                }

                Music = Music2;
                SetPhase((int)Phase + 1);
            }

            // Update Goozma's shoot sound loop.
            goozmaShootPowerTarget = Utils.GetLerpValue(PhaseTransitionSplit_ReformDelay + 40f, PhaseTransitionSplit_OverallDuration - 60f, Time, true) * 1.2f;
            goozmaShootPowerCurrent = goozmaShootPowerTarget;
        }

        /// <summary>
        /// Performs Goozma's effects in preparation for his second phase.
        /// </summary>
        /// 
        /// <remarks>
        /// It is expected that this method will be called exclusively server-side. Sync accordingly.
        /// </remarks>
        public void PerformPhase2TransitionEffects()
        {
            NPC.dontTakeDamage = false;
            NPC.defense += Phase2TransitionDefenseBoost;
            NPC.netUpdate = true;
        }

        /// <summary>
        /// Moves the camera onto Goozma during his phase transition split.
        /// </summary>
        public void DoBehavior_PhaseTransitionSplit_UpdateCamera()
        {
            if (Time < PhaseTransitionSplit_OverallDuration) {
                FocusConditional.focusTarget = NPC.Center;
            }
            if (Time >= 1 && !initializedLocalP2Camera) {

                if (Main.netMode != NetmodeID.Server) {
                    int panOut = Main.masterMode && Main.getGoodWorld ? 600 : 30;
                    Main.instance.CameraModifiers.Add(new FocusConditional(30, panOut, () => Time < PhaseTransitionSplit_CameraPanReturnTime, "Goozma"));

                    SoundStyle boomSound = AssetDirectory.Sounds.Goozma.Explode;
                    SoundEngine.PlaySound(boomSound, NPC.Center);
                }

                initializedLocalP2Camera = true;
            }
        }

        /// <summary>
        /// Modifies Goozma's simmer sound during his phase transition split.
        /// </summary>
        public void DoBehavior_PhaseTransitionSplit_HandleGoozmaSimmerSound(ref float pitch, ref float volume)
        {
            float soundRiseInterpolant = Utils.GetLerpValue(PhaseTransitionSplit_ReformDelay + 30f, PhaseTransitionSplit_OverallDuration - 20f, Time, true);
            pitch = soundRiseInterpolant;
            volume = MathHelper.Lerp(0.5f, 1.1f, soundRiseInterpolant);
        }

        /// <summary>
        /// Performs custom rendering for Goozma's phase transition split animation.
        /// </summary>
        public void DoBehavior_PhaseTransitionSplit_CustomRendering(float eyeRotation, Color glowColor, Vector2 drawPosition, ref float eyeScale)
        {
            SpriteBatch spriteBatch = Main.spriteBatch;
            Texture2D glow = AssetDirectory.Textures.Glow[0].Value;
            Texture2D sclera = AssetDirectory.Textures.Goozma.Sclera.Value;
            Texture2D godEye = AssetDirectory.Textures.ChromaticSoulEye.Value;

            if (Phase == 1 && Time >= PhaseTransitionSplit_ExplodeTime) {
                eyeScale *= MathF.Cbrt(Utils.GetLerpValue(-10f, 30f, Time - PhaseTransitionSplit_ReformDelay, true));
                float eyeFlash = Utils.GetLerpValue(0f, 60f, Time - PhaseTransitionSplit_ReformDelay, true);

                spriteBatch.Draw(godEye, drawPosition, godEye.Frame(), glowColor * (1f - eyeFlash) * 0.5f, eyeRotation, godEye.Size() * 0.5f, eyeScale * (1f + eyePower.Length() * 0.06f + eyeFlash * 3f), 0, 0);
                spriteBatch.Draw(godEye, drawPosition, godEye.Frame(), glowColor * (1f - eyeFlash) * 0.2f, eyeRotation, godEye.Size() * 0.5f, eyeScale * (1f + eyePower.Length() * 0.06f + eyeFlash * 9f), 0, 0);

                spriteBatch.Draw(sclera, drawPosition, sclera.Frame(), Color.White, 0, sclera.Size() * 0.5f, 1.05f, 0, 0);
                spriteBatch.Draw(glow, drawPosition, glow.Frame(), glowColor * 0.5f, extraTilt + NPC.rotation, glow.Size() * 0.5f, 1.2f, 0, 0);
            }
        }
    }
}
