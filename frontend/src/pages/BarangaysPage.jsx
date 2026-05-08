import CrudPage from '../components/CrudPage'

const fields = [
  { name: 'cityId', label: 'City Id', type: 'int', required: true },
  { name: 'barangayCode', label: 'Barangay Code', type: 'text', required: true },
  { name: 'barangayName', label: 'Barangay Name', type: 'text', required: true },
  { name: 'createdAt', label: 'Created At', type: 'datetime-local', inForm: false },
]

export default function BarangaysPage() {
  return <CrudPage title="Barangays" endpoint="/Barangays" idField="barangayId" fields={fields} />
}
