import React, { useState } from "react";
import { useRegisterSqlConnection } from "../hooks/useDataSources";
import { sqlConnectionSchema } from "@shared/lib/validation/schemas";
import { Button } from "@shared/ui/Button/Button";

export interface SqlConnectionFormProps {
  onConnected?: (schema: import("@shared/types/api-contracts").ColumnSchemaDto[]) => void;
}

export const SqlConnectionForm: React.FC<SqlConnectionFormProps> = ({ onConnected }) => {
  const { mutate, isPending } = useRegisterSqlConnection();
  const [provider, setProvider] = useState<"sqlserver" | "postgresql" | "mysql">("sqlserver");
  const [form, setForm] = useState({ host: "", database: "", username: "", password: "" });
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    const parsed = sqlConnectionSchema.safeParse(form);
    if (!parsed.success) {
      setErrorMsg("Please fill out all connection fields properly.");
      return;
    }
    setErrorMsg(null);

    const connStr = provider === "postgresql"
      ? `Host=${form.host};Database=${form.database};Username=${form.username};Password=${form.password};`
      : provider === "mysql"
      ? `Server=${form.host};Database=${form.database};Uid=${form.username};Pwd=${form.password};`
      : `Server=${form.host};Database=${form.database};User Id=${form.username};Password=${form.password};`;

    mutate(
      {
        name: `${form.database}@${form.host}`,
        connectionString: connStr,
        type: provider
      },
      {
        onSuccess: (result) => {
          if (result.schema && result.schema.length > 0) {
            onConnected?.(result.schema);
          }
        },
        onError: (err) => {
          setErrorMsg(err.message);
        }
      }
    );
  };

  return (
    <form onSubmit={handleSubmit} aria-label="sql-connection-form" style={{ display: "flex", flexDirection: "column", gap: "12px", maxWidth: "500px" }}>
      <div className="form-group" style={{ marginBottom: "0.25rem" }}>
        <label className="form-label">Database Provider</label>
        <select
          className="form-select"
          value={provider}
          onChange={(e) => setProvider(e.target.value as "sqlserver" | "postgresql" | "mysql")}
          aria-label="sql-provider-select"
        >
          <option value="sqlserver">Microsoft SQL Server</option>
          <option value="postgresql">PostgreSQL</option>
          <option value="mysql">MySQL</option>
        </select>
      </div>

      {(["host", "database", "username", "password"] as const).map((field) => (
        <div key={field} className="form-group" style={{ marginBottom: "0.25rem" }}>
          <label className="form-label" style={{ textTransform: "capitalize" }}>{field}</label>
          <input
            className="form-input"
            placeholder={`Enter ${field}`}
            type={field === "password" ? "password" : "text"}
            value={form[field]}
            onChange={(e) => setForm({ ...form, [field]: e.target.value })}
            aria-label={`sql-${field}-input`}
          />
        </div>
      ))}

      {errorMsg && (
        <div style={{ padding: "0.5rem", background: "var(--danger-bg)", color: "var(--danger)", borderRadius: "var(--radius-sm)", fontSize: "0.875rem" }}>
          {errorMsg}
        </div>
      )}

      <Button type="submit" disabled={isPending} aria-label="connect-database-button">
        {isPending ? "Connecting & Extracting Schema..." : "Connect Database"}
      </Button>
    </form>
  );
};
