import React, { useState } from "react";
import { useRegisterSqlConnection } from "../hooks/useDataSources";
import { sqlConnectionSchema } from "@shared/lib/validation/schemas";
import { Button } from "@shared/ui/Button/Button";

export const SqlConnectionForm: React.FC = () => {
  const { mutate, isPending } = useRegisterSqlConnection();
  const [form, setForm] = useState({ host: "", database: "", username: "", password: "" });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    const parsed = sqlConnectionSchema.safeParse(form);
    if (!parsed.success) return;

    mutate({
      name: `${form.database}@${form.host}`,
      connectionString: `Server=${form.host};Database=${form.database};User Id=${form.username};Password=${form.password};`,
      type: "sqlserver"
    });
  };

  return (
    <form onSubmit={handleSubmit} aria-label="sql-connection-form">
      {(["host", "database", "username", "password"] as const).map((field) => (
        <input
          key={field}
          placeholder={field}
          type={field === "password" ? "password" : "text"}
          value={form[field]}
          onChange={(e) => setForm({ ...form, [field]: e.target.value })}
        />
      ))}
      <Button type="submit" disabled={isPending}>
        Connect
      </Button>
    </form>
  );
};
