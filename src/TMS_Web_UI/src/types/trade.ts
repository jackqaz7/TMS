export type CreateTradeRequest = {
  currencyPair: string
  notionalAmount: number
  tradeRate: number
  valueDate: string
}

export type TradeSummary = {
  tradeId: number
  currencyPair: string
  notionalAmount: number
  tradeRate: number
  status: string
}
