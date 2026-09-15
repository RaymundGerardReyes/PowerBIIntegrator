export const routePaths = {
  login: "/login",
  dashboards: "/dashboards",
  dashboardDetail: (id: string) => `/dashboards/${id}`,
  dataSources: "/data-sources",
  reports: "/reports"
} as const;
