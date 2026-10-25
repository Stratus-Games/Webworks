class AccountError extends Error {
  constructor(code, status = 400) {
    super(code);
    this.name = 'AccountError';
    this.code = code;
    this.status = status;
  }
}

module.exports = { AccountError };
