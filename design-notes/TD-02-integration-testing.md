# Tech Debt 02 - Integration Testing

- Instead of relying on the prior test to clean up properly. Have each test be its own docker image. but only build dottie once.
- add parameter for verbosity to the integration test script, allowing the ai agent to be able to get smaller output to fit the context window
- add parameter for test selection to the integration test script, allowing the ai agent to focus on one things at a time. 
- Review the existing integration tests and ensure that:
    - All aspects of the dottie's CLI interface are tested
    - All tests are properly formed and don't take short cuts just to pass the test.
- identify additional scenarios and add those tests are part of the integration test.
    - where dottie would be run multiple times
    - where config changes and dottie is run again
    - where multiple types of install are used at once
    - where link and install are used together
    - etc
