export const useWorkflowStepResults = () => {
  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('is-IS', {
      style: 'currency',
      currency: 'ISK',
      minimumFractionDigits: 0
    }).format(amount)
  }

  const formatStepResult = (stepType: string, result: Record<string, any>): string => {
    if (!result) return ''
    
    // Generic formatting based on common patterns
    if (result.MatchCount !== undefined) {
      return `Fann ${result.MatchCount} niðurstöður`
    }
    if (result.ProposedCreditAmount !== undefined) {
      return formatCurrency(result.ProposedCreditAmount)
    }
    if (result.MatchConfidence !== undefined) {
      return `Áreiðanleiki: ${(result.MatchConfidence * 100).toFixed(0)}%`
    }
    // Return empty string for unknown result types
    return ''
  }
  
  return { formatStepResult, formatCurrency }
}

