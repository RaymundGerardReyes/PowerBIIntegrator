Feature: Advisory Unlock And Audit Workflow
  As a Data Steward
  I want to unlock confidential sample rows for an AI advisory query
  So that the advisory tier provides grounded explanations while strictly forcing local execution and auditing the unlock

  Scenario: Requesting confidential exposure and verifying local execution and audit
    Given a completed pipeline run with confidential sample rows
    When a DataSteward requests to unlock confidential exposure with a valid audit reason
    Then the unlock request should succeed with an audit token
    And subsequent advisory query for that run should execute locally via LocalOllama
    And the advisory response should contain verified rule citations

