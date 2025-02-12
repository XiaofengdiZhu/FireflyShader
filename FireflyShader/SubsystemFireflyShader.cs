using Engine;
using Engine.Graphics;
using GameEntitySystem;
using TemplatesDatabase;
using Color = Engine.Color;

namespace Game {
    public class SubsystemFireflyShader : Subsystem, IDrawable {
        public FireflyShaderFlatBatch m_batch;

        public Vector3 m_lastPlayerPosition;
        public static int FireflyCount = 65536;
        public int[] DrawOrders => [105];

        public override void Load(ValuesDictionary valuesDictionary) {
            m_batch = new FireflyShaderFlatBatch();
            GLWrapper.UseProgram(FireflyShaderFlatBatch.FireflyShaderShader.m_computeProgram);
            m_batch.FireflyBuffer.SetData(1, new Vector4[FireflyCount], 0, FireflyCount);
            Random random = new();
            float[] noise = new float[196608];
            for (int i = 0; i < 196608; i++) {
                noise[i] = random.Float();
            }
            m_batch.FireflyBuffer.SetData(2, noise, 0, 196608);
        }

        public void Draw(Camera camera, int drawOrder) {
            m_batch.SetDelta(m_lastPlayerPosition == default ? default : camera.ViewPosition - m_lastPlayerPosition, Window.m_lastRenderDelta);
            m_lastPlayerPosition = camera.ViewPosition;
            m_batch.QueueFirefly(camera.ViewPosition, Color.Green);
            m_batch.Flush(camera.ViewProjectionMatrix, default);
        }
    }
}