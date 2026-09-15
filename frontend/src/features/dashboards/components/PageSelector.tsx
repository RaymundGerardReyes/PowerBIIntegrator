import React from "react";
import type { Page } from "../model/types";

interface PageSelectorProps {
  pages: Page[];
  selected: string;
  onSelect: (pageName: string) => void;
}

export const PageSelector: React.FC<PageSelectorProps> = ({ pages, selected, onSelect }) => (
  <ul role="tablist">
    {pages.map((page) => (
      <li key={page.name}>
        <button role="tab" aria-selected={page.name === selected} onClick={() => onSelect(page.name)}>
          {page.name}
        </button>
      </li>
    ))}
  </ul>
);
