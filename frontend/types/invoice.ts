export interface InvoiceItem {
    id: string;
    invoiceId: string;
    itemName: string;
    itemId: string;
    quantity: number;
    unit: string;
    unitPrice: number; // Net Price
    listPrice: number; // Original Price
    vatCode: string;   // AA, S11 etc
    discount: number;
    totalPrice: number;
    totalPriceWithVat: number;
}

export interface Invoice {
    id: string;
    supplierName: string;
    buyerName?: string;
    buyerTaxId?: string;
    invoiceNumber: string;
    invoiceDate: string;
    totalAmount: number;
    items: InvoiceItem[];
    createdAt: string;
}
