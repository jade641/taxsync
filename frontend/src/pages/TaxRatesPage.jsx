import CrudPage from '../components/CrudPage'

const fields = [
  {
    name: 'propertyType',
    label: 'Property Type',
    type: 'select',
    options: ['Residential', 'Commercial', 'Industrial', 'Agricultural', 'MixedUse'],
    required: true,
  },
  { name: 'ratePercentage', label: 'Rate Percentage', type: 'number', required: true },
  { name: 'effectiveFrom', label: 'Effective From', type: 'date', required: true },
  { name: 'effectiveTo', label: 'Effective To', type: 'date' },
  { name: 'description', label: 'Description', type: 'textarea' },
  { name: 'createdBy', label: 'Created By', type: 'int' },
  { name: 'createdAt', label: 'Created At', type: 'datetime-local', inForm: false },
]

export default function TaxRatesPage() {
  return <CrudPage title="Tax Rates" endpoint="/TaxRates" idField="rateId" fields={fields} />
}
