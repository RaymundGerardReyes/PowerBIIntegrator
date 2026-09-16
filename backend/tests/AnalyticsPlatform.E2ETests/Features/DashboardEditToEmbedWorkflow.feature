Feature: Dashboard Edit To Embed Workflow
  As a business user
  I want to publish a dashboard and retrieve an embed token
  So that I can view interactive reports in the web client

  Scenario: Publishing a dashboard and retrieving embed config
    Given a dashboard definition with visuals and layout bounds
    When I publish the dashboard to Fabric workspace
    Then the embed configuration should be retrievable
    And the embed config should contain a valid reportId and embedUrl

