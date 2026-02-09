namespace UrbanEcho.Tests;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Test1()
    {
        ProjectFile? projectFile = null;
        try
        {
            ProjectFile.Open("testfile");
        }
        catch (Exception ex)
        {
            Assert.Fail($"Project file didn't open with error {ex.ToString()}");
        }

        if (projectFile == null)
        {
            Assert.Fail($"Project file null");
        }
    }
}