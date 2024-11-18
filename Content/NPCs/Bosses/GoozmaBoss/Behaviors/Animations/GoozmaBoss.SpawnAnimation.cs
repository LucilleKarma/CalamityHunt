using System;
using CalamityHunt.Common.Systems.Particles;
using CalamityHunt.Common.Utilities;
using CalamityHunt.Content.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.Chat;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityHunt.Content.NPCs.Bosses.GoozmaBoss
{
    public partial class Goozma : ModNPC
    {
        /// <summary>
        /// How long Goozma sits idly during his spawn animation, before roaring.
        /// </summary>
        public static int SpawnAnimation_AwakenDelay => 120;

        /// <summary>
        /// How long Goozma spends roaring during his spawn animation.
        /// </summary>
        public static int SpawnAnimation_RoarTime => 70;

        /// <summary>
        /// Performs Goozma's spawn animation.
        /// </summary>
        public void DoBehavior_SpawnAnimation()
        {
            int awakenDelay = SpawnAnimation_AwakenDelay;

            SetAttack(AttackList.SpawnSelf);
            NPC.direction = -1;
            eyePower = Vector2.One * 0.8f;

            // Sit in place, mostly inert, before awakening.
            if (Time < awakenDelay) {
                float flyDownwardInterpolant = Utils.GetLerpValue(awakenDelay * 0.2f, awakenDelay, Time, true);
                NPC.velocity.Y = MathHelper.Lerp(-0.5f, 3.5f, flyDownwardInterpolant);
                NPC.direction = NPC.velocity.X > 0 ? 1 : -1;

                for (int i = 0; i < Main.musicFade.Length; i++) {
                    Main.musicFade[i] = 0f;
                }
                try {
                    Main.musicFade[Main.curMusic] = 0f;
                    Main.musicFade[Main.newMusic] = 0f;
                }
                catch (IndexOutOfRangeException) {

                }
            }

            // Ensure that residual motion from the inert pre-phase is gracefully cleared shortly before and after Goozma awakens.
            if (Time > awakenDelay - 5) {
                NPC.velocity *= 0.9f;
                eyeOpen = true;
            }

            if (Time == awakenDelay) {
                DoBehavior_SpawnAnimation_Initialize();
            }

            // Slightly enigmatic section and comment. Likely to do with frustrations with multiplayer.
            // Will leave untouched, out of an abundance of caution. -Lucille
            if (Time >= awakenDelay && !initializedLocal) { // i must fix the music AAAAAAAAAAAAAAAAAAAAAAAAAAamf
                Music = Music1;
                if (Main.netMode != NetmodeID.Server) {
                    Main.newMusic = Music1;
                    try {
                        Main.musicFade[Main.curMusic] = 0f;
                        Main.musicFade[Main.newMusic] = 1f;
                    }
                    catch (IndexOutOfRangeException) { }
                }

                SoundEngine.PlaySound(AssetDirectory.Sounds.Goozma.Awaken.WithVolumeScale(1.8f), NPC.Center);

                initializedLocal = true;
            }

            // Shake the screen after Goozma awakens, to go along with his spawn animation.
            if (Time > awakenDelay && Time % 4 == 0) {
                Main.instance.CameraModifiers.Add(new PunchCameraModifier(NPC.Center, Main.rand.NextVector2CircularEdge(1, 1), 20f, 6f, 10, 8000, "Goozma"));
            }

            // Begin the fight after awakening and roaring for a sufficient quantity of time.
            if (Time > awakenDelay + SpawnAnimation_RoarTime) {
                SetPhase(0);
                NPC.dontTakeDamage = false;
            }

            if (Main.netMode != NetmodeID.Server) {
                Dust sludge = Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(10, 10), DustID.TintableDust, Main.rand.NextVector2CircularEdge(10, 10), 200, Color.Black, Main.rand.NextFloat(2f, 4f));
                sludge.noGravity = true;
            }

            if (Main.netMode != NetmodeID.Server && Main.rand.NextBool((int)(Time * 0.1f + 2))) {
                Vector2 particleVelocity = Vector2.UnitY.RotatedByRandom(1f);
                particleVelocity.Y -= Main.rand.NextFloat(3f);

                CalamityHunt.particles.Add(Particle.Create<ChromaticGooBurst>(particle => {
                    particle.position = Main.rand.NextVector2FromRectangle(NPC.Hitbox);
                    particle.velocity = particleVelocity + particle.position.DirectionFrom(NPC.Center);
                    particle.scale = Main.rand.NextFloat(0.5f, 1.5f);
                    particle.color = new GradientColor(SlimeUtils.GoozColors, 0.2f, 0.2f).Value;
                }));
            }
        }

        /// <summary>
        /// Performs awaken initialization effects for Goozma during his spawn animation, including things such as starting his music and creating the "Goozma has awoken!" chat text.
        /// </summary>
        public void DoBehavior_SpawnAnimation_Initialize()
        {
            NPC.netUpdate = true;
            eyeOpen = true;

            Music = Music1;

            if (Main.netMode == NetmodeID.SinglePlayer) {
                Main.NewText(Language.GetTextValue("Announcement.HasAwoken", NPC.TypeName), HUtils.BossTextColor);
            }
            else if (Main.netMode == NetmodeID.Server) {
                ChatHelper.BroadcastChatMessage(NetworkText.FromKey("Announcement.HasAwoken", NPC.GetTypeNetName()), HUtils.BossTextColor);
            }
        }
    }
}
