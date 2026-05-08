import CrudPage from '../components/CrudPage'

const fields = [
  { name: 'regionCode', label: 'Region Code', type: 'text', required: true },
  { name: 'regionName', label: 'Region Name', type: 'text', required: true },
  { name: 'description', label: 'Description', type: 'textarea' },
  { name: 'createdAt', label: 'Created At', type: 'datetime-local', inForm: false },
]

export default function RegionsPage() {
  return <CrudPage title="Regions" endpoint="/Regions" idField="regionId" fields={fields} />
}
