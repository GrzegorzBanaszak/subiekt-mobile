export class ApiRequestError extends Error {
  constructor(public readonly status: number, detail?: string) {
    super(detail || `API request failed with status ${status}.`)
  }
}
