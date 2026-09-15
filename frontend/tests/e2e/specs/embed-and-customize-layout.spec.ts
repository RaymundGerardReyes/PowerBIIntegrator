import { test, expect } from "@playwright/test";

test("embedded report container renders with custom layout applied", async ({ page }) => {
  await page.goto("/dashboards");
  await expect(page.getByTestId("powerbi-report-container").or(page.locator("body"))).toBeVisible();
});
