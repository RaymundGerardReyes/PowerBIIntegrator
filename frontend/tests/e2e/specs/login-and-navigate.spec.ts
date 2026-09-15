import { test, expect } from "@playwright/test";
import { testUsers } from "../fixtures/testUsers";

test("user logs in and reaches the dashboards page", async ({ page }) => {
  await page.goto("/login");
  await page.getByPlaceholder("Email").fill(testUsers.standardUser.email);
  await page.getByPlaceholder("Password").fill(testUsers.standardUser.password);
  await page.getByRole("button", { name: "Sign in" }).click();
  await expect(page).toHaveURL(/dashboards/);
});
