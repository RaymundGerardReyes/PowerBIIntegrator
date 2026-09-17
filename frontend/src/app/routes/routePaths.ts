export const routePaths = {
  login: "/login",
  dashboards: "/dashboards",
  dashboardDetail: (id: string) => `/dashboards/${id}`,
  dataSources: "/data-sources",
  dataQuality: "/data-quality",
  reports: "/reports"
} as const;
