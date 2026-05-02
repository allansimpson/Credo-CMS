import { clsx, type ClassValue } from "clsx";
import { twMerge } from "tailwind-merge";

/**
 * shadcn-style className helper. Combines clsx and tailwind-merge so callers
 * can compose conditional class lists without worrying about Tailwind class
 * conflicts.
 */
export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}
