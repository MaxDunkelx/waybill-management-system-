// Formatting utilities for dates, currency, etc.

export const formatters = {
  formatDate: (dateString: string): string => {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  },

  formatDateTime: (dateString: string): string => {
    const date = new Date(dateString);
    return date.toLocaleString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  },

  formatCurrency: (amount: number | null | undefined, currency: string = 'ILS'): string => {
    const safeAmount = amount ?? 0;
    return new Intl.NumberFormat('he-IL', {
      style: 'currency',
      currency: currency
    }).format(safeAmount);
  },

  formatNumber: (value: number | null | undefined, decimals: number = 2): string => {
    const safeValue = value ?? 0;
    return safeValue.toFixed(decimals);
  },

  formatQuantity: (quantity: number | null | undefined, unit: string): string => {
    const safeQuantity = quantity ?? 0;
    return `${safeQuantity.toFixed(2)} ${unit}`;
  }
};
