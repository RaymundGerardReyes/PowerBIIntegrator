import React from "react";
import type { Page } from "../model/types";

interface PageSelectorProps {
  pages: Page[];
  selected: string;
  onSelect: (pageName: string) => void;
  onAddPage?: () => void;
  onDeletePage?: (pageName: string) => void;
}

export const PageSelector: React.FC<PageSelectorProps> = ({
  pages,
  selected,
  onSelect,
  onAddPage,
  onDeletePage
}) => {
  return (
    <div
      role="tablist"
      style={{
        display: "flex",
        alignItems: "center",
        gap: "0.25rem",
        overflowX: "auto",
        maxWidth: "100%",
        padding: "2px"
      }}
    >
      {pages.map((page) => {
        const isSelected = page.name === selected;
        return (
          <div
            key={page.name}
            style={{
              display: "flex",
              alignItems: "center",
              borderRadius: "6px",
              backgroundColor: isSelected ? "var(--primary-tint, #eff6ff)" : "var(--bg-subtle, #f8fafc)",
              border: isSelected ? "1px solid var(--primary, #3b82f6)" : "1px solid var(--border-color, #e2e8f0)",
              transition: "all 0.15s ease"
            }}
          >
            <button
              role="tab"
              aria-selected={isSelected}
              aria-label={page.name}
              data-testid={`page-tab-${page.name}`}
              onClick={() => onSelect(page.name)}
              style={{
                background: "none",
                border: "none",
                padding: "4px 10px",
                fontSize: "0.8125rem",
                fontWeight: isSelected ? 600 : 400,
                color: isSelected ? "var(--primary, #2563eb)" : "var(--text-secondary, #475569)",
                cursor: "pointer",
                whiteSpace: "nowrap"
              }}
            >
              {page.name}
            </button>
            {pages.length > 1 && onDeletePage && (
              <button
                aria-label={`delete-page-${page.name}`}
                data-testid={`delete-page-${page.name}`}
                onClick={(e) => {
                  e.stopPropagation();
                  onDeletePage(page.name);
                }}
                style={{
                  background: "none",
                  border: "none",
                  padding: "4px 6px 4px 0",
                  fontSize: "0.85rem",
                  color: "var(--text-muted, #94a3b8)",
                  cursor: "pointer",
                  lineHeight: 1
                }}
                title="Delete this page"
              >
                ×
              </button>
            )}
          </div>
        );
      })}

      {onAddPage && (
        <button
          data-testid="btn-add-page"
          onClick={onAddPage}
          style={{
            display: "flex",
            alignItems: "center",
            gap: "0.25rem",
            padding: "4px 10px",
            fontSize: "0.75rem",
            fontWeight: 500,
            borderRadius: "6px",
            backgroundColor: "var(--bg-card, #ffffff)",
            border: "1px dashed var(--border-color, #cbd5e1)",
            color: "var(--text-secondary, #64748b)",
            cursor: "pointer",
            whiteSpace: "nowrap",
            transition: "all 0.15s ease"
          }}
          title="Add a new canvas report page"
        >
          <span>+</span> New Page
        </button>
      )}
    </div>
  );
};
