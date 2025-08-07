import createEdgeClient from "@honeycomb-protocol/edge-client";

const API_URL = "https://edge.test.honeycombprotocol.com/";

export const client = createEdgeClient(API_URL, true);

// Store your project address here after creating the project
// You can get this from the createCreateProjectTransaction response or Solana Explorer
// Your transaction: iCCNk6BEBHwy4HVbXVNthMsuc7TD36zgsH2eoKwK5SEewg9idw32SP165ZMSFN6F6kHcwViFjW8gDNjMPsMuQtR
export const PROJECT_ADDRESS = "6bKfwmbgbWH38JFKzXmczkY4YRYqKxZEdV3VM6HkRmWg"; // TODO: Add your project address from the transaction above
