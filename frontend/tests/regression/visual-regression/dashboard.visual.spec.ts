import { test, expect } from "@playwright/test";

test("dashboard canvas visual regression", async ({ page }) => {
  await page.goto("/dashboards");
  await expect(page).toHaveScreenshot("dashboard-canvas.png");
});
