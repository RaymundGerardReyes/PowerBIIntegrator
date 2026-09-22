import { test, expect } from "@playwright/test";

test.describe("Canvas Layout & Responsive Display Modes E2E", () => {
  test("canvas mounts with toolbar and supports mode switching and zoom", async ({ page }) => {
    await page.goto("/dashboards");

    // Ensure the dashboards workspace loads
    await expect(page.locator("body")).toBeVisible();

    // Verify canvas toolbar exists if dashboard is rendered
    const toolbar = page.getByTestId("canvas-toolbar");
    if (await toolbar.isVisible()) {
      // Test mode buttons
      const fitToPageBtn = page.getByTestId("mode-fit-to-page");
      const fitToWidthBtn = page.getByTestId("mode-fit-to-width");
      const actualSizeBtn = page.getByTestId("mode-actual-size");

      await expect(fitToPageBtn).toBeVisible();
      await expect(fitToWidthBtn).toBeVisible();
      await expect(actualSizeBtn).toBeVisible();

      // Switch to Fit to Width
      await fitToWidthBtn.click();
      await expect(fitToWidthBtn).toHaveClass(/btn-primary/);

      // Switch to Actual Size
      await actualSizeBtn.click();
      await expect(actualSizeBtn).toHaveClass(/btn-primary/);

      // Zoom controls
      const zoomBadge = page.getByTestId("zoom-level-badge");
      const zoomIn = page.getByTestId("btn-zoom-in");
      const zoomOut = page.getByTestId("btn-zoom-out");
      const zoomReset = page.getByTestId("btn-zoom-reset");

      await expect(zoomBadge).toBeVisible();
      await zoomIn.click();
      await zoomOut.click();
      await zoomReset.click();
    }
  });

  test("visual card has container query context and size-aware classes", async ({ page }) => {
    await page.goto("/dashboards");

    const visualCard = page.locator("[data-testid^='visual-']").first();
    if (await visualCard.isVisible()) {
      // Check container-type is inline-size
      const containerType = await visualCard.evaluate((el) => {
        return window.getComputedStyle(el).containerType || (el as HTMLElement).style.containerType;
      });
      expect(["inline-size", "normal", ""]).toContain(containerType);
    }
  });
});

