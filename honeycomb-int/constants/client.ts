import createEdgeClient from "@honeycomb-protocol/edge-client";

const API_URL = "https://edge.test.honeycombprotocol.com/";

export const client = createEdgeClient(API_URL, true);

export const PROJECT_ADDRESS = "6bKfwmbgbWH38JFKzXmczkY4YRYqKxZEdV3VM6HkRmWg"; 