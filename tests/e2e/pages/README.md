# Page Object Model Convention

All shared UI affordances live in this directory.

## Structure

```
pages/
├── BasePage.ts           # Common navigation, headers, footers
├── DeviceListPage.ts     # Device list/browse page selectors & actions
├── DeviceFormPage.ts     # Create/edit device form
├── SignInPage.ts         # Sign-in flow (Entra ID)
└── AdminPage.ts          # Reference data admin pages
```

## Rules

- Page objects expose **actions**, not selectors
- Test files describe **scenarios**, not low-level clicks
- Selectors prefer `data-testid` > `role` > `text` > CSS class
- No business logic in page objects — they're navigation helpers
- Keep page objects lean; extract reusable components as needed

## Example

```typescript
// Good
await deviceListPage.filterByCategory('Laptop');
await expect(deviceListPage.firstDevice).toContainText('MacBook Pro');

// Bad
await page.locator('[data-testid="category-filter"]').click();
await page.locator('option:has-text("Laptop")').click();
await expect(page.locator('.device-list .device:first-child')).toContainText('MacBook Pro');
```
