import React, { useState, useMemo, useCallback } from "react";
import type { Visual } from "@entities/visual/types";
import { cleanFieldLabel, validateVisualRoles } from "@entities/measure";

export interface SlicerVisualProps {
  visual: Visual;
  onSelectionChange?: (selectedIds: string[]) => void;
}

export interface SlicerOption {
  id: string;
  label: string;
  count?: number;
}

export function getDimensionOptions(field: string): SlicerOption[] {
  const clean = cleanFieldLabel(field).toLowerCase();

  if (clean === "pclass" || clean === "class" || clean === "tier") {
    return [
      { id: "1", label: "Pclass 1", count: 323 },
      { id: "2", label: "Pclass 2", count: 277 },
      { id: "3", label: "Pclass 3", count: 709 }
    ];
  }

  if (clean === "sex" || clean === "gender") {
    return [
      { id: "male", label: "Male", count: 843 },
      { id: "female", label: "Female", count: 466 }
    ];
  }

  if (clean.includes("embark") || clean.includes("port")) {
    return [
      { id: "S", label: "Southampton (S)", count: 914 },
      { id: "C", label: "Cherbourg (C)", count: 270 },
      { id: "Q", label: "Queenstown (Q)", count: 123 }
    ];
  }

  if (clean.includes("region")) {
    return [
      { id: "north-america", label: "North America", count: 540 },
      { id: "europe", label: "Europe", count: 420 },
      { id: "asia-pacific", label: "Asia-Pacific", count: 260 },
      { id: "latin-america", label: "Latin America", count: 89 }
    ];
  }

  if (clean.includes("status")) {
    return [
      { id: "active", label: "Active", count: 720 },
      { id: "pending", label: "Pending", count: 310 },
      { id: "closed", label: "Closed", count: 180 },
      { id: "archived", label: "Archived", count: 99 }
    ];
  }

  if (clean.includes("category") || clean.includes("channel")) {
    return [
      { id: "online", label: "Online", count: 650 },
      { id: "retail", label: "Retail / Store", count: 430 },
      { id: "direct", label: "Direct Sales", count: 180 },
      { id: "partner", label: "Partner Network", count: 49 }
    ];
  }

  if (clean.includes("surviv") || clean.includes("target")) {
    return [
      { id: "1", label: "Survived (1)", count: 500 },
      { id: "0", label: "Did Not Survive (0)", count: 809 }
    ];
  }

  const baseLabel = cleanFieldLabel(field) || "Item";
  return [
    { id: `${baseLabel}-1`, label: `${baseLabel} 1`, count: 240 },
    { id: `${baseLabel}-2`, label: `${baseLabel} 2`, count: 180 },
    { id: `${baseLabel}-3`, label: `${baseLabel} 3`, count: 120 },
    { id: `${baseLabel}-4`, label: `${baseLabel} 4`, count: 75 }
  ];
}

export const SlicerVisual: React.FC<SlicerVisualProps> = ({ visual, onSelectionChange }) => {
  const roleValidation = validateVisualRoles(visual.visualType, visual.boundFields);

  const boundField = visual.boundFields?.[0] ?? "";
  const rawDimensionName = cleanFieldLabel(boundField);
  const dimensionTitle = rawDimensionName || "Dimension Filter";

  const allOptions = useMemo(() => getDimensionOptions(boundField), [boundField]);

  // Initial state: all items selected by default (unfiltered slicer)
  const [selectedIds, setSelectedIds] = useState<string[]>(() => allOptions.map((o) => o.id));
  const [searchQuery, setSearchQuery] = useState("");

  // Synchronize selectedIds whenever the bound dimension changes
  const prevFieldRef = React.useRef(boundField);
  React.useEffect(() => {
    if (prevFieldRef.current !== boundField) {
      prevFieldRef.current = boundField;
      setSelectedIds(allOptions.map((o) => o.id));
    }
  }, [boundField, allOptions]);

  const filteredOptions = useMemo(() => {
    if (!searchQuery.trim()) return allOptions;
    const query = searchQuery.toLowerCase();
    return allOptions.filter(
      (opt) => opt.label.toLowerCase().includes(query) || opt.id.toLowerCase().includes(query)
    );
  }, [allOptions, searchQuery]);

  const handleToggle = useCallback(
    (id: string) => {
      const next = selectedIds.includes(id)
        ? selectedIds.filter((item) => item !== id)
        : [...selectedIds, id];
      setSelectedIds(next);
      onSelectionChange?.(next);
    },
    [selectedIds, onSelectionChange]
  );

  const handleSelectAll = useCallback(() => {
    const next = allOptions.map((o) => o.id);
    setSelectedIds(next);
    onSelectionChange?.(next);
  }, [allOptions, onSelectionChange]);

  const handleClear = useCallback(() => {
    setSelectedIds([]);
    onSelectionChange?.([]);
  }, [onSelectionChange]);

  if (!roleValidation.isValid) {
    return (
      <div
        data-testid={`slicer-error-${visual.name}`}
        style={{
          display: "flex",
          flexDirection: "column",
          justifyContent: "center",
          alignItems: "center",
          height: "100%",
          padding: "1rem",
          backgroundColor: "var(--danger-bg, #fef2f2)",
          border: "1px dashed var(--danger, #ef4444)",
          borderRadius: "var(--radius-sm, 6px)",
          boxSizing: "border-box",
          textAlign: "center"
        }}
      >
        <span style={{ fontSize: "1.25rem", color: "var(--danger, #ef4444)" }}>⚠️</span>
        <div style={{ fontWeight: 700, fontSize: "0.8rem", color: "var(--danger, #b91c1c)", marginTop: "0.25rem" }}>
          Invalid Slicer Dimension Binding
        </div>
        <p style={{ fontSize: "0.7rem", color: "var(--text-secondary, #6b7280)", margin: "0.25rem 0 0 0" }}>
          {roleValidation.error}
        </p>
      </div>
    );
  }

  const allSelected = allOptions.length > 0 && allOptions.every((opt) => selectedIds.includes(opt.id));
  const isNoneSelected = selectedIds.length === 0;

  return (
    <div
      data-testid={`slicer-${visual.name}`}
      className="slicer-visual-container"
      style={{
        display: "flex",
        flexDirection: "column",
        height: "100%",
        boxSizing: "border-box",
        overflow: "hidden",
        padding: "0.25rem 0.35rem",
        fontSize: "0.75rem",
        color: "var(--text-primary, #111827)"
      }}
    >
      {/* Slicer Header & Subtitle */}
      <div
        style={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: "center",
          marginBottom: "0.35rem",
          paddingBottom: "0.25rem",
          borderBottom: "1px solid var(--border-color, #e5e7eb)",
          flexShrink: 0
        }}
      >
        <div style={{ display: "flex", alignItems: "center", gap: "0.25rem", minWidth: 0 }}>
          <span style={{ fontSize: "0.8rem" }}>🎛️</span>
          <span
            style={{
              fontWeight: 600,
              fontSize: "0.75rem",
              whiteSpace: "nowrap",
              overflow: "hidden",
              textOverflow: "ellipsis"
            }}
            title={dimensionTitle}
          >
            {dimensionTitle}
          </span>
        </div>
        <span
          data-testid="slicer-selection-summary"
          style={{
            fontSize: "0.65rem",
            color: allSelected ? "var(--primary, #2563eb)" : "var(--text-muted, #6b7280)",
            backgroundColor: "var(--bg-subtle, #f3f4f6)",
            padding: "1px 6px",
            borderRadius: "9999px",
            fontWeight: 500,
            flexShrink: 0
          }}
        >
          {allSelected ? "All" : isNoneSelected ? "None" : `${selectedIds.length}/${allOptions.length}`}
        </span>
      </div>

      {/* Slicer Filter Bar & Quick Actions */}
      <div
        style={{
          display: "flex",
          alignItems: "center",
          gap: "0.25rem",
          marginBottom: "0.35rem",
          flexShrink: 0
        }}
      >
        <input
          type="text"
          data-testid="slicer-search-input"
          placeholder="Filter..."
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
          style={{
            flex: 1,
            minWidth: 0,
            fontSize: "0.6875rem",
            padding: "2px 6px",
            borderRadius: "var(--radius-sm, 4px)",
            border: "1px solid var(--border-color, #d1d5db)",
            backgroundColor: "var(--bg-surface, #ffffff)",
            color: "var(--text-primary, #111827)",
            outline: "none"
          }}
        />
        <button
          type="button"
          data-testid="slicer-select-all"
          onClick={handleSelectAll}
          title="Select all items"
          style={{
            fontSize: "0.65rem",
            padding: "2px 5px",
            borderRadius: "var(--radius-sm, 4px)",
            border: "1px solid var(--border-color, #d1d5db)",
            backgroundColor: "var(--bg-subtle, #f9fafb)",
            cursor: "pointer",
            color: "var(--text-secondary, #374151)"
          }}
        >
          All
        </button>
        <button
          type="button"
          data-testid="slicer-clear"
          onClick={handleClear}
          title="Clear selections"
          style={{
            fontSize: "0.65rem",
            padding: "2px 5px",
            borderRadius: "var(--radius-sm, 4px)",
            border: "1px solid var(--border-color, #d1d5db)",
            backgroundColor: "var(--bg-subtle, #f9fafb)",
            cursor: "pointer",
            color: "var(--text-secondary, #374151)"
          }}
        >
          Clear
        </button>
      </div>

      {/* Interactive Dimension Items List */}
      <div
        data-testid="slicer-items-list"
        style={{
          flex: 1,
          overflowY: "auto",
          display: "flex",
          flexDirection: "column",
          gap: "0.25rem",
          paddingRight: "2px"
        }}
      >
        {filteredOptions.length === 0 ? (
          <div
            style={{
              padding: "0.75rem",
              textAlign: "center",
              color: "var(--text-muted, #9ca3af)",
              fontSize: "0.6875rem"
            }}
          >
            No matches found
          </div>
        ) : (
          filteredOptions.map((opt) => {
            const isSelected = selectedIds.includes(opt.id);
            return (
              <div
                key={opt.id}
                data-testid={`slicer-item-${opt.id}`}
                role="checkbox"
                aria-checked={isSelected}
                tabIndex={0}
                onClick={() => handleToggle(opt.id)}
                onKeyDown={(e) => {
                  if (e.key === "Enter" || e.key === " ") {
                    e.preventDefault();
                    handleToggle(opt.id);
                  }
                }}
                style={{
                  display: "flex",
                  alignItems: "center",
                  justifyContent: "space-between",
                  padding: "0.25rem 0.5rem",
                  borderRadius: "var(--radius-sm, 4px)",
                  backgroundColor: isSelected
                    ? "var(--primary-subtle, #eff6ff)"
                    : "var(--bg-surface, #ffffff)",
                  border: isSelected
                    ? "1px solid var(--primary, #3b82f6)"
                    : "1px solid var(--border-color, #e5e7eb)",
                  cursor: "pointer",
                  userSelect: "none",
                  transition: "background-color 0.15s ease, border-color 0.15s ease"
                }}
              >
                <div style={{ display: "flex", alignItems: "center", gap: "0.375rem", minWidth: 0 }}>
                  <input
                    type="checkbox"
                    checked={isSelected}
                    readOnly
                    tabIndex={-1}
                    aria-hidden="true"
                    style={{ cursor: "pointer", margin: 0 }}
                  />
                  <span
                    style={{
                      fontSize: "0.725rem",
                      fontWeight: isSelected ? 600 : 400,
                      color: isSelected ? "var(--primary-text, #1d4ed8)" : "var(--text-primary, #111827)",
                      whiteSpace: "nowrap",
                      overflow: "hidden",
                      textOverflow: "ellipsis"
                    }}
                  >
                    {opt.label}
                  </span>
                </div>
                {opt.count !== undefined && (
                  <span
                    style={{
                      fontSize: "0.625rem",
                      color: "var(--text-muted, #9ca3af)",
                      backgroundColor: isSelected ? "rgba(59, 130, 246, 0.1)" : "var(--bg-subtle, #f3f4f6)",
                      padding: "1px 5px",
                      borderRadius: "9999px",
                      marginLeft: "0.25rem",
                      flexShrink: 0
                    }}
                  >
                    {opt.count.toLocaleString()}
                  </span>
                )}
              </div>
            );
          })
        )}
      </div>
    </div>
  );
};
