export const useWorkflowStepNames = () => {
  const stepNameMap: Record<string, string> = {
    OrderLookup: 'Leita að pöntun',
    OrderVerification: 'Staðfesta pöntun',
    CreditCalculation: 'Reikna inneign',
    ResponseDraft: 'Drög að svari',
    Approval: 'Samþykki',
    CreditIssuance: 'Úthluta inneign',
    EmailSend: 'Senda svar',
    BookingAvailabilityCheck: 'Athuga lausn',
    BookingResponseDraft: 'Drög að bókunarsvari',
    // Add new step types here
  }
  
  const getStepDisplayName = (stepType: string): string => {
    return stepNameMap[stepType] || stepType
  }
  
  return { getStepDisplayName, stepNameMap }
}

