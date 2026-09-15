import { z } from "zod";

export const measureSchema = z.object({
  name: z.string().min(1).max(128),
  expression: z.string().min(1),
  tableName: z.string().min(1)
});

export const sqlConnectionSchema = z.object({
  host: z.string().min(1),
  database: z.string().min(1),
  username: z.string().min(1),
  password: z.string().min(1)
});
