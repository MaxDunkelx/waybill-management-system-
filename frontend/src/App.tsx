import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { TenantSelector } from './components/TenantSelector';
import { WaybillList } from './components/WaybillList';
import { WaybillDetails } from './components/WaybillDetails';
import { WaybillImport } from './components/WaybillImport';
import { WaybillSummary } from './components/WaybillSummary';
import { SupplierSummary } from './components/SupplierSummary';
import { Reports } from './components/Reports';
import { Layout } from './components/Layout';
import './styles/components.css';

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/tenant-select" element={<TenantSelector />} />
        <Route path="/" element={<Layout />}>
          <Route index element={<Navigate to="/waybills" replace />} />
          <Route path="waybills" element={<WaybillList />} />
          <Route path="waybills/:id" element={<WaybillDetails />} />
          <Route path="import" element={<WaybillImport />} />
          <Route path="summary" element={<WaybillSummary />} />
          <Route path="suppliers/:id/summary" element={<SupplierSummary />} />
          <Route path="reports" element={<Reports />} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
}

export default App;
