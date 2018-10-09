using CommandLine;
using NUnit.Framework;
using System.IO;
using MachineLearning.Sampling;

namespace SamplingUnitTest
{
    [TestFixture]
    public class CleanSamplingTest
    {
        [Test]
        public void TestCleanSampling()
        {
            string modelPath = Path.GetFullPath(Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "..//..//.."))
            + Path.DirectorySeparatorChar + "ExampleFiles"
            + Path.DirectorySeparatorChar + "VariabilityModelSampling.xml";
            if (!File.Exists(modelPath))
            {
                modelPath = "/home/travis/build/se-passau/SPLConqueror/SPLConqueror/Example"
                  + "Files/VariabilityModelSampling.xml";
            }
            Commands cmd = new Commands();
            assertNoSamplingStrategies(cmd);
            cmd.performOneCommand(Commands.COMMAND_VARIABILITYMODEL + " " + modelPath);
            cmd.performOneCommand(ConfigurationBuilder.COMMAND_SAMPLE_FEATUREWISE);
            cmd.performOneCommand(ConfigurationBuilder.COMMAND_SAMPLE_BINARY_TWISE + " " + ConfigurationBuilder.COMMAND_VALIDATION);
            cmd.performOneCommand(ConfigurationBuilder.COMMAND_EXPERIMENTALDESIGN + " " + ConfigurationBuilder.COMMAND_EXPDESIGN_FULLFACTORIAL);
            cmd.performOneCommand(ConfigurationBuilder.COMMAND_EXPERIMENTALDESIGN + " " + ConfigurationBuilder.COMMAND_EXPDESIGN_BOXBEHNKEN
                + " " + ConfigurationBuilder.COMMAND_VALIDATION);
            cmd.performOneCommand(Commands.COMMAND_CLEAR_SAMPLING);
            assertNoSamplingStrategies(cmd);
        }

        private void assertNoSamplingStrategies(Commands cmd)
        {
            Assert.AreEqual(0, cmd.configBuilder.numericStrategies.Count);
            Assert.AreEqual(0, cmd.configBuilder.numericStrategiesValidation.Count);
            Assert.AreEqual(0, cmd.configBuilder.binaryStrategies.Count);
            Assert.AreEqual(0, cmd.configBuilder.binaryStrategiesValidation.Count);
        }
    }
}
