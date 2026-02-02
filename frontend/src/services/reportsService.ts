import apiClient from './api';
import type { MonthlyReportResult } from '../types/api';

export const reportsService = {
  // Generate monthly report
  async generateMonthlyReport(year: number, month: number): Promise<MonthlyReportResult> {
    const response = await apiClient.post('/api/Reports/generate-monthly-report', {
      year,
      month,
    });
    return response.data;
  },
};
