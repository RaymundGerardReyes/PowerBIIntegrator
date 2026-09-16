Feature: Full Publish Workflow
  As an analytics developer
  I want to compile and package my analytics model into PBIP and TMDL artifacts
  So that it can be published to Microsoft Fabric

  Scenario: Compiling and downloading a PBIP archive end-to-end
    Given an analytics model with valid tables and relationships
    When I request to compile and download the PBIP archive
    Then the response should contain a valid zip archive
    And the zip archive should contain PBIR and TMDL definitions

