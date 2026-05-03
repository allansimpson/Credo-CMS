import { render, screen } from "@testing-library/react";
import { describe, expect, it, beforeEach } from "vitest";
import { ResponsiveTable, type ColumnDef } from "./ResponsiveTable";

interface Row {
  id: string;
  name: string;
  email: string;
}

const sampleData: Row[] = [
  { id: "1", name: "Ada Lovelace", email: "ada@example.com" },
  { id: "2", name: "Grace Hopper", email: "grace@example.com" },
];

const columns: ColumnDef<Row>[] = [
  { id: "name", header: "Name", accessor: (r) => r.name, mobilePriority: 1 },
  { id: "email", header: "Email", accessor: (r) => r.email, mobilePriority: 2 },
];

function setWidth(w: number) {
  Object.defineProperty(window, "innerWidth", { writable: true, configurable: true, value: w });
}

describe("<ResponsiveTable>", () => {
  beforeEach(() => {
    setWidth(1440);
  });

  it("renders a real <table> on desktop", () => {
    setWidth(1440);
    render(
      <ResponsiveTable
        data={sampleData}
        columns={columns}
        rowKey={(r) => r.id}
        searchable={false}
      />,
    );
    window.dispatchEvent(new Event("resize"));
    expect(document.querySelector("table")).toBeInTheDocument();
    expect(screen.getByText("ada@example.com")).toBeInTheDocument();
  });

  it("renders cards (no <table>) on mobile", () => {
    setWidth(375);
    render(
      <ResponsiveTable
        data={sampleData}
        columns={columns}
        rowKey={(r) => r.id}
        searchable={false}
      />,
    );
    window.dispatchEvent(new Event("resize"));
    expect(screen.getByText("Ada Lovelace")).toBeInTheDocument();
    expect(screen.getByText("grace@example.com")).toBeInTheDocument();
  });
});
