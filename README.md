# 3020

## TODO
* Write a nice readme
  * Document algorithm
    * Preferably using excalidraw (Jake)
    * NOTE: Document how we handle required courses / the rule for those.
  * Document usage of dependencies
    * NOTE: Make it clear that these are just tools to make testing and output easier not libraries critical or core to our implementation.
    * Document usage of SpectreConsole (Jake)
    * Document usage of Verify (Kara)
* Finish Scheduler (Jake)
  * Placement
* Generate larger test data (Kara)
  * It would be nice if we could try and parse the courses.json into our course data format to get some more tailored test data.
* Testing
  * Description
    * For testing we are going to be using mstest
      * dotnet new mstest -o A1Tests
      * Setup github actions testing
        * https://github.com/spotandjake/amod-4901/blob/main/.github/workflows/checks.yaml
        * We can almost use the tests I have above
      * For snapshot testing we will use `Verify`
        * https://github.com/VerifyTests/Verify
  * Test Part A Requirements (Kara)
    * Test each method
  * Test Part B Requirements (Undecided)
    * Test how we handle leaf nodes
    * Test CoRequisites
    * Test PreRequisites
    * At least like 10 smaller checks targeting each edge case directly
    * A few massive snapshot tests to make sure everything is working together