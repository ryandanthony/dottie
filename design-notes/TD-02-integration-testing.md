# Tech Debt 02 - Integration Testing


## Summary
Integrations tests are going to drive the qwuality of the software more than unit tests. We need to take the time to ensure that they are higher quality and cover all scenarios. 

## Proposed work

- Instead of relying on the prior test to clean up properly. Have each test be its own docker image. but only build dottie once.
- improvements to allow ai agent to run tests and get better/correct results faster.
    - add parameter for verbosity to the integration test script, allowing the ai agent to be able to get smaller output to fit the context window
    - add parameter for test selection to the integration test script, allowing the ai agent to focus on one things at a time. 
    - Are there other improvments?
- Review the existing integration tests and ensure that:
    - all tests are passing
    - All aspects of the dottie's CLI interface are tested
    - All tests are properly formed and don't take short cuts just to pass the test.
- identify additional scenarios and add those tests are part of the integration test.
    - where dottie would be run multiple times
    - where config changes and dottie is run again
    - where multiple types of install are used at once
    - where link and install are used together
    - etc
- identify edge cases where there could be failures, and create test secnarios that will trigger the scenarios

