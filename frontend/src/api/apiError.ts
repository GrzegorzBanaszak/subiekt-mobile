export class ApiRequestError extends Error {
  constructor(public readonly status: number) {
    super(`API request failed with status ${status}.`)
  }
}
