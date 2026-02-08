namespace A1Tests;

[TestClass]
public class UnitTest1 : VerifyBase {
  [TestMethod]
  public void TestMethod1() {
    // Placeholder for Part A/B tests
  }

  [TestMethod]
  public Task SnapshotExample() {
    return this.Verify("Hello, snapshot!");
  }
}
