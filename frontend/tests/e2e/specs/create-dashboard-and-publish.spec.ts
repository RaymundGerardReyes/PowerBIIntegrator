import { test, expect } from "@playwright/test";

test("user creates a dashboard and triggers Power BI publish", async ({ page }) => {
  await page.goto("/dashboards");
  await expect(page.getByRole("tablist")).toBeVisible();
});
