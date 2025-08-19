'use client'

import React, { useState } from "react";
import { useWallet } from "@solana/wallet-adapter-react";
import { sendClientTransactions } from "@honeycomb-protocol/edge-client/client/walletHelpers";
import { client, PROJECT_ADDRESS } from "../constants/client";

// Add the ResourceStorageEnum import - you may need to adjust this import path
// based on your honeycomb client version
import { ResourceStorageEnum } from "@honeycomb-protocol/edge-client";

// FIXED: Use the resource that's actually updating when you win games!
const POINTS_RESOURCE_ADDRESS = "xpbAFftHPRQEUoy5V7rDQ7WnzzXeQSZWwiCTLHSU9vY";

// Store your resource tree address here after creating it
const RESOURCE_TREE_ADDRESS = "7gLh4fF8TPhftojqe5ZpHagUbuNQwdMvz7PXWmuJMJJK";

export default function Home() {
  const wallet = useWallet();
  const [isLoading, setIsLoading] = useState(false);
  const [response, setResponse] = useState<any>(null);
  const [projectAddress, setProjectAddress] = useState(PROJECT_ADDRESS);
  const [pointsResourceAddress, setPointsResourceAddress] = useState(POINTS_RESOURCE_ADDRESS);
  const [resourceTreeAddress, setResourceTreeAddress] = useState(RESOURCE_TREE_ADDRESS);

  const createProject = async () => {
    if (!wallet.publicKey) {
      alert("Please connect your wallet first");
      return;
    }

    setIsLoading(true);
    try {
      const {
        createCreateProjectTransaction: { project, tx: txResponse }
      } = await client.createCreateProjectTransaction({
        name: "My Project",
        authority: 'DjwMoX4hs6tNgScT1zSy52ApxGbn8UCY3kpFcqkh23XA',
      });

      const result = await sendClientTransactions(
        client,
        wallet,
        txResponse
      );

      // Store the project address
      setProjectAddress(project);
      setResponse({ ...result, projectAddress: project });
      console.log("Project created with address:", project);
    } catch (error) {
      console.error("Error creating project:", error);
      alert("Error creating project: " + (error as Error).message);
    } finally {
      setIsLoading(false);
    }
  };

  const createProfilesTree = async () => {
    if (!wallet.publicKey) {
      alert("Please connect your wallet first");
      return;
    }
    if (!projectAddress) {
      alert("Please create a project first");
      return;
    }

    setIsLoading(true);
    try {
      const {
        createCreateProfilesTreeTransaction: { tx: txResponse }
      } = await client.createCreateProfilesTreeTransaction({
        payer: wallet.publicKey.toString(),
        project: projectAddress.toString(),
        treeConfig: {
          basic: {
            numAssets: 100000, // Can store 100,000 profiles
          },
        }
      });

      const result = await sendClientTransactions(
        client,
        wallet,
        txResponse
      );

      setResponse(result);
      console.log("Profiles tree created:", result);
    } catch (error) {
      console.error("Error creating profiles tree:", error);
      alert("Error creating profiles tree: " + (error as Error).message);
    } finally {
      setIsLoading(false);
    }
  };

  // Create Points Resource
  const createPointsResource = async () => {
    if (!wallet.publicKey) {
      alert("Please connect your wallet first");
      return;
    }
    if (!projectAddress) {
      alert("Please create a project first");
      return;
    }

    setIsLoading(true);
    try {
      const {
        createCreateNewResourceTransaction: {
          resource: resourceAddress,
          tx: txResponse
        }
      } = await client.createCreateNewResourceTransaction({
        project: projectAddress,
        authority: 'DjwMoX4hs6tNgScT1zSy52ApxGbn8UCY3kpFcqkh23XA',
        payer: wallet.publicKey.toString(),
        params: {
          name: "Points",
          decimals: 0, // Points are whole numbers
          symbol: "PTS",
          uri: "https://example.com/points-metadata", // You can replace with your own metadata URI
          storage: ResourceStorageEnum.LedgerState, // Compressed storage
          tags: ["Points", "Currency", "Reward"], // Optional tags for categorization
        }
      });

      const result = await sendClientTransactions(
        client,
        wallet,
        txResponse
      );

      // Store the resource address
      setPointsResourceAddress(resourceAddress);
      setResponse({ 
        ...result, 
        resourceAddress: resourceAddress,
        type: "Resource Creation"
      });
      console.log("Points resource created with address:", resourceAddress);
    } catch (error) {
      console.error("Error creating points resource:", error);
      alert("Error creating points resource: " + (error as Error).message);
    } finally {
      setIsLoading(false);
    }
  };

  // Create Resource Tree for Points
  const createPointsResourceTree = async () => {
    if (!wallet.publicKey) {
      alert("Please connect your wallet first");
      return;
    }
    if (!projectAddress) {
      alert("Please create a project first");
      return;
    }
    if (!pointsResourceAddress) {
      alert("Please create the points resource first");
      return;
    }

    setIsLoading(true);
    try {
      const {
        createCreateNewResourceTreeTransaction: {
          treeAddress: merkleTreeAddress,
          tx: txResponse
        }
      } = await client.createCreateNewResourceTreeTransaction({
        project: projectAddress,
        authority: 'DjwMoX4hs6tNgScT1zSy52ApxGbn8UCY3kpFcqkh23XA',
        payer: wallet.publicKey.toString(),
        resource: pointsResourceAddress,
        treeConfig: {
          basic: {
            numAssets: 100000, // The tree can store 100,000 compressed resource records
          },
        }
      });

      const result = await sendClientTransactions(
        client,
        wallet,
        txResponse
      );

      // Store the tree address
      setResourceTreeAddress(merkleTreeAddress);
      setResponse({ 
        ...result, 
        treeAddress: merkleTreeAddress,
        resourceAddress: pointsResourceAddress,
        type: "Resource Tree Creation"
      });
      console.log("Points resource tree created with address:", merkleTreeAddress);
    } catch (error) {
      console.error("Error creating points resource tree:", error);
      alert("Error creating points resource tree: " + (error as Error).message);
    } finally {
      setIsLoading(false);
    }
  };

  const createUser = async () => {
    if (!wallet.publicKey) {
      alert("Please connect your wallet first");
      return;
    }

    setIsLoading(true);
    try {
      const {
        createNewUserTransaction: txResponse
      } = await client.createNewUserTransaction({
        wallet: wallet.publicKey.toString(),
        info: {
          name: "Test User",
          pfp: "https://lh3.googleusercontent.com/-Jsm7S8BHy4nOzrw2f5AryUgp9Fym2buUOkkxgNplGCddTkiKBXPLRytTMXBXwGcHuRr06EvJStmkHj-9JeTfmHsnT0prHg5Mhg",
          bio: "This is a test user created through the Honeycomb integration",
        },
        payer: wallet.publicKey.toString(),
      });

      const result = await sendClientTransactions(
        client,
        wallet,
        txResponse
      );

      setResponse(result);
      console.log("User created:", result);
    } catch (error) {
      console.error("Error creating user:", error);
      alert("Error creating user: " + (error as Error).message);
    } finally {
      setIsLoading(false);
    }
  };

  const createUserWithProfile = async () => {
    if (!wallet.publicKey) {
      alert("Please connect your wallet first");
      return;
    }
    if (!projectAddress) {
      alert("Please create a project and profiles tree first");
      return;
    }

    setIsLoading(true);
    try {
      const {
        createNewUserWithProfileTransaction: txResponse
      } = await client.createNewUserWithProfileTransaction({
        project: projectAddress.toString(),
        wallet: wallet.publicKey.toString(),
        payer: wallet.publicKey.toString(),
        profileIdentity: "main",
        userInfo: {
          name: "Honeycomb Developer",
          bio: "This user is created for testing purposes",
          pfp: "https://lh3.googleusercontent.com/-Jsm7S8BHy4nOzrw2f5AryUgp9Fym2buUOkkxgNplGCddTkiKBXPLRytTMXBXwGcHuRr06EvJStmkHj-9JeTfmHsnT0prHg5Mhg",
        },
      });

      const result = await sendClientTransactions(
        client,
        wallet,
        txResponse
      );

      setResponse(result);
      console.log("User and profile created:", result);
    } catch (error) {
      console.error("Error creating user with profile:", error);
      alert("Error creating user with profile: " + (error as Error).message);
    } finally {
      setIsLoading(false);
    }
  };

  // Mint 10 Points (Win Reward) - Fixed Version
  const mintWinReward = async () => {
    if (!wallet.publicKey) {
      alert("Please connect your wallet first");
      return;
    }
    if (!pointsResourceAddress) {
      alert("Please create the points resource first");
      return;
    }

    setIsLoading(true);
    try {
      console.log("🏆 Minting points...");
      console.log("Resource:", pointsResourceAddress);
      console.log("Authority:", wallet.publicKey.toString());
      console.log("Owner (recipient):", wallet.publicKey.toString());

      const { 
        createMintResourceTransaction: txResponse
      } = await client.createMintResourceTransaction({
        resource: pointsResourceAddress.toString(),
        amount: "10", // 10 points for winning
        authority: 'DjwMoX4hs6tNgScT1zSy52ApxGbn8UCY3kpFcqkh23XA', // You (the project authority) are minting
        owner: wallet.publicKey.toString(), // Give points to yourself (or change this to another user's wallet)
        payer: wallet.publicKey.toString(),
      });

      console.log("📝 Transaction created, sending...");
      
      const result = await sendClientTransactions(
        client,
        wallet,
        txResponse
      );

      console.log("✅ Transaction result:", result);

      setResponse({ 
        ...result, 
        pointsMinted: "10",
        recipient: wallet.publicKey.toString(),
        type: "Points Minted - Win Reward",
        mintDetails: {
          resource: pointsResourceAddress,
          amount: "10"
        }
      });
      console.log("🎉 10 points minted for winning!");
      
      // Wait a moment then auto-check balance
      setTimeout(() => {
        console.log("🔄 Auto-checking balance after mint...");
        checkPointBalance();
      }, 3000);

    } catch (error) {
      console.error("❌ Error minting win reward:", error);
      alert("Error minting win reward: " + (error as Error).message);
    } finally {
      setIsLoading(false);
    }
  };

  // Check User's Point Balance - Shows the ACTUAL balance from the updating resource
  const checkPointBalance = async () => {
    if (!wallet.publicKey) {
      alert("Please connect your wallet first");
      return;
    }
  
    setIsLoading(true);
    try {
      console.log("🔍 Searching for holdings...");
      console.log("Holder:", wallet.publicKey.toString());
  
      // Get ALL user holdings first
      const allHoldings = await client.findHoldings({
        holders: [wallet.publicKey.toString()]
      });
  
      console.log("🔍 All user holdings:", allHoldings);
      const allHoldingsArray = (allHoldings as any)?.holdings || [];
  
      // Find the specific resource that's actually updating (BqnYp7MLyi9rPCQ9Wb3pWmDZEdFv7LWu6EEwoiJQgpwc)
      const targetResource = "BqnYp7MLyi9rPCQ9Wb3pWmDZEdFv7LWu6EEkoiJQgpwc";
      let actualBalance = "0";
      let foundResource = null;
      
      allHoldingsArray.forEach((holding: any, index: number) => {
        console.log(`${index + 1}. Resource: ${holding.address}`);
        console.log(`   Balance: ${holding.balance}`);
        console.log(`   Is Points Resource? ${holding.address === pointsResourceAddress}`);
        console.log(`   Is Updating Resource? ${holding.address === targetResource}`);
        
        // Use the resource that matches our expected points resource
        if (holding.address === pointsResourceAddress) {
          actualBalance = holding.balance;
          foundResource = holding.address;
        }
      });

      // Try compressed search with the resource tree
      let compressedBalance = "0";
      if (resourceTreeAddress) {
        try {
          const compressedHoldings = await client.findHoldings({
            holders: [wallet.publicKey.toString()],
            trees: [resourceTreeAddress]
          });
          const compressedArray = (compressedHoldings as any)?.holdings || [];
          if (compressedArray.length > 0) {
            compressedBalance = compressedArray[0]?.balance || "0";
            console.log("🔍 Compressed holdings balance:", compressedBalance);
          }
        } catch (error) {
          console.log("⚠️ Could not check compressed holdings:", error);
        }
      }

      const displayBalance = Math.max(parseInt(actualBalance), parseInt(compressedBalance));
  
      setResponse({
        type: "✅ Point Balance Check",
        holder: wallet.publicKey.toString(),
        currentPointsBalance: displayBalance.toString(),
        pointsResourceUsed: foundResource || pointsResourceAddress,
        allResourceBalances: allHoldingsArray.map((h: any) => ({
          resource: h.address,
          balance: h.balance,
          isActivePointsResource: h.address === pointsResourceAddress
        })),
        compressedBalance: compressedBalance,
        summary: {
          message: displayBalance > 0 ? 
            `🎉 You have ${displayBalance} points!` :
            `❌ No points found. Try minting first.`,
          resourceAddress: pointsResourceAddress,
          actualBalance: actualBalance
        }
      });
  
    } catch (error) {
      console.error("Error checking point balance:", error);
      setResponse({
        type: "Point Balance Check Error",
        error: (error as Error).message,
        holder: wallet.publicKey.toString()
      });
    } finally {
      setIsLoading(false);
    }
  };

  // Debug - Check what resources you're actually minting to
  const debugMintLocation = async () => {
    if (!wallet.publicKey) {
      alert("Please connect your wallet first");
      return;
    }

    setIsLoading(true);
    try {
      console.log("🔍 Checking your project resources...");
      
      // Get all resources for your project
      const projectResources = await client.findResources({
        projects: [projectAddress]
      });

      console.log("📋 Project Resources:", projectResources);

      setResponse({
        type: "Debug - Project Resources",
        project: projectAddress,
        resources: projectResources,
        currentPointsResource: pointsResourceAddress,
        analysis: {
          message: "These are ALL the resources in your project. Your minting might be going to one of these instead of the expected address.",
          yourHoldings: "You have tokens in 5 different resources",
          recommendation: "Compare these project resources with your holdings to find the correct resource address"
        }
      });

    } catch (error) {
      console.error("Error checking project resources:", error);
      setResponse({
        type: "Debug Error",
        error: (error as Error).message
      });
    } finally {
      setIsLoading(false);
    }
  };

  const checkUserExists = async () => {
    if (!wallet.publicKey) {
      alert("Please connect your wallet first");
      return;
    }

    setIsLoading(true);
    try {
      const usersResult = await client.findUsers({
        wallets: [wallet.publicKey.toString()]
      });
      const user = (usersResult as any)?.[0] || null;

      setResponse({
        type: "User Query",
        exists: !!user,
        user: user || null,
        walletAddress: wallet.publicKey.toString()
      });
      console.log("User query result:", user);
    } catch (error) {
      console.error("Error checking user:", error);
      setResponse({
        type: "User Query",
        exists: false,
        error: (error as Error).message,
        walletAddress: wallet.publicKey.toString()
      });
    } finally {
      setIsLoading(false);
    }
  };

  const checkUserProfile = async () => {
    if (!wallet.publicKey) {
      alert("Please connect your wallet first");
      return;
    }
    if (!projectAddress) {
      alert("Please create a project first");
      return;
    }

    setIsLoading(true);
    try {
      const profilesResult = await client.findProfiles({
        projects: [projectAddress],
        identities: ["main"]
      });
      const profile = (profilesResult as any)?.[0] || null;

      setResponse({
        type: "Profile Query",
        exists: !!profile,
        profile: profile || null,
        project: projectAddress,
        walletAddress: wallet.publicKey.toString(),
        identity: "main"
      });
      console.log("Profile query result:", profile);
    } catch (error) {
      console.error("Error checking profile:", error);
      setResponse({
        type: "Profile Query",
        exists: false,
        error: (error as Error).message,
        project: projectAddress,
        walletAddress: wallet.publicKey.toString()
      });
    } finally {
      setIsLoading(false);
    }
  };

  const getAllProjects = async () => {
    if (!wallet.publicKey) {
      alert("Please connect your wallet first");
      return;
    }

    setIsLoading(true);
    try {
      const projects = await client.findProjects({
        authorities: [wallet.publicKey.toString()]
      });

      setResponse({
        type: "Projects Query",
        count: (projects as any)?.project?.length || 0,
        projects: (projects as any)?.project || [],
        authority: 'DjwMoX4hs6tNgScT1zSy52ApxGbn8UCY3kpFcqkh23XA'
      });
      console.log("Projects query result:", projects);
    } catch (error) {
      console.error("Error getting projects:", error);
      setResponse({
        type: "Projects Query",
        error: (error as Error).message,
        authority: 'DjwMoX4hs6tNgScT1zSy52ApxGbn8UCY3kpFcqkh23XA'
      });
    } finally {
      setIsLoading(false);
    }
  };

  // Get Project Resources
  const getProjectResources = async () => {
    if (!wallet.publicKey) {
      alert("Please connect your wallet first");
      return;
    }
    if (!projectAddress) {
      alert("Please create a project first");
      return;
    }

    setIsLoading(true);
    try {
      const resources = await client.findResources({
        projects: [projectAddress]
      });

      setResponse({
        type: "Resources Query",
        count: (resources as any)?.length || 0,
        resources: resources || [],
        project: projectAddress
      });
      console.log("Resources query result:", resources);
    } catch (error) {
      console.error("Error getting resources:", error);
      setResponse({
        type: "Resources Query",
        error: (error as Error).message,
        project: projectAddress
      });
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <main className="flex min-h-screen flex-col items-center p-8">
      <div className="w-full max-w-5xl">
        <h1 className="text-4xl font-bold text-center mb-8">Honeycomb Integration - Fixed Version</h1>

        {projectAddress && (
          <div className="mb-6 p-4 bg-green-100 rounded">
            <h3 className="font-bold text-green-800">Project Address:</h3>
            <p className="text-sm text-green-700 font-mono">{projectAddress}</p>
          </div>
        )}

        {pointsResourceAddress && (
          <div className="mb-6 p-4 bg-yellow-100 rounded">
            <h3 className="font-bold text-yellow-800">Points Resource Address (ACTIVE - Updates on Win):</h3>
            <p className="text-sm text-yellow-700 font-mono">{pointsResourceAddress}</p>
            <p className="text-xs text-yellow-600 mt-1">✅ This resource balance updates when you win games</p>
          </div>
        )}

        {resourceTreeAddress && (
          <div className="mb-6 p-4 bg-red-100 rounded">
            <h3 className="font-bold text-red-800">Resource Tree Address:</h3>
            <p className="text-sm text-red-700 font-mono">{resourceTreeAddress}</p>
          </div>
        )}

        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 mb-8">
          {/* Step 1: Create Project */}
          <div className="p-6 border rounded-lg">
            <h2 className="text-xl font-bold mb-3">1. Create Project</h2>
            <p className="text-sm text-gray-600 mb-4">
              First step: Create a Honeycomb project
            </p>
            <button
              onClick={createProject}
              disabled={!wallet.publicKey || isLoading}
              className="w-full px-4 py-2 bg-blue-500 text-white rounded hover:bg-blue-600 disabled:bg-gray-400"
            >
              {isLoading ? "Creating..." : "Create Project"}
            </button>
          </div>

          {/* Step 2: Create Profiles Tree */}
          <div className="p-6 border rounded-lg">
            <h2 className="text-xl font-bold mb-3">2. Create Profiles Tree</h2>
            <p className="text-sm text-gray-600 mb-4">
              Set up storage for user profiles (100K capacity)
            </p>
            <button
              onClick={createProfilesTree}
              disabled={!wallet.publicKey || !projectAddress || isLoading}
              className="w-full px-4 py-2 bg-purple-500 text-white rounded hover:bg-purple-600 disabled:bg-gray-400"
            >
              {isLoading ? "Creating..." : "Create Profiles Tree"}
            </button>
          </div>

          {/* Step 3: Create Points Resource */}
          <div className="p-6 border rounded-lg">
            <h2 className="text-xl font-bold mb-3">3. Create Points Resource</h2>
            <p className="text-sm text-gray-600 mb-4">
              Create a points resource for your game/app
            </p>
            <button
              onClick={createPointsResource}
              disabled={!wallet.publicKey || !projectAddress || isLoading}
              className="w-full px-4 py-2 bg-yellow-500 text-white rounded hover:bg-yellow-600 disabled:bg-gray-400"
            >
              {isLoading ? "Creating..." : "Create Points Resource"}
            </button>
          </div>

          {/* Step 4: Create Resource Tree */}
          <div className="p-6 border rounded-lg">
            <h2 className="text-xl font-bold mb-3">4. Create Resource Tree</h2>
            <p className="text-sm text-gray-600 mb-4">
              Create compressed storage for the points resource (100K capacity)
            </p>
            <button
              onClick={createPointsResourceTree}
              disabled={!wallet.publicKey || !projectAddress || !pointsResourceAddress || isLoading}
              className="w-full px-4 py-2 bg-red-500 text-white rounded hover:bg-red-600 disabled:bg-gray-400"
            >
              {isLoading ? "Creating..." : "Create Points Tree"}
            </button>
          </div>

          {/* Step 5: Create User Only */}
          <div className="p-6 border rounded-lg">
            <h2 className="text-xl font-bold mb-3">5. Create User</h2>
            <p className="text-sm text-gray-600 mb-4">
              Create a universal user account
            </p>
            <button
              onClick={createUser}
              disabled={!wallet.publicKey || isLoading}
              className="w-full px-4 py-2 bg-green-500 text-white rounded hover:bg-green-600 disabled:bg-gray-400"
            >
              {isLoading ? "Creating..." : "Create User"}
            </button>
          </div>

          {/* Step 6: Create User + Profile */}
          <div className="p-6 border rounded-lg">
            <h2 className="text-xl font-bold mb-3">6. Create User + Profile</h2>
            <p className="text-sm text-gray-600 mb-4">
              Create user and project profile in one transaction
            </p>
            <button
              onClick={createUserWithProfile}
              disabled={!wallet.publicKey || !projectAddress || isLoading}
              className="w-full px-4 py-2 bg-orange-500 text-white rounded hover:bg-orange-600 disabled:bg-gray-400"
            >
              {isLoading ? "Creating..." : "Create User + Profile"}
            </button>
          </div>
        </div>

        {/* Gameplay Section */}
        <div className="mb-8 p-6 bg-green-800 rounded-lg">
          <h2 className="text-xl font-bold mb-4">🎮 Gameplay Actions</h2>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <button
              onClick={mintWinReward}
              disabled={!wallet.publicKey || !pointsResourceAddress || isLoading}
              className="px-6 py-3 bg-green-500 text-white rounded-lg hover:bg-green-600 disabled:bg-gray-400 font-bold"
            >
              {isLoading ? "Minting..." : "🏆 Win Game (+10 Points)"}
            </button>

            <button
              onClick={checkPointBalance}
              disabled={!wallet.publicKey || isLoading}
              className="px-6 py-3 bg-blue-500 text-white rounded-lg hover:bg-blue-600 disabled:bg-gray-400 font-bold"
            >
              {isLoading ? "Checking..." : "💰 Check My Points"}
            </button>

            <button
              onClick={debugMintLocation}
              disabled={!wallet.publicKey || !projectAddress || isLoading}
              className="px-6 py-3 bg-purple-500 text-white rounded-lg hover:bg-purple-600 disabled:bg-gray-400 font-bold"
            >
              {isLoading ? "Debugging..." : "🔧 Debug: Find My Resources"}
            </button>
          </div>
        </div>

        {/* Verification Section */}
        <div className="mb-8 p-6 bg-gray-800 rounded-lg">
          <h2 className="text-xl font-bold mb-4">🔍 Verification & Queries</h2>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
            <button
              onClick={checkUserExists}
              disabled={!wallet.publicKey || isLoading}
              className="px-4 py-2 bg-cyan-500 text-white rounded hover:bg-cyan-600 disabled:bg-gray-400"
            >
              {isLoading ? "Checking..." : "Check User Exists"}
            </button>

            <button
              onClick={checkUserProfile}
              disabled={!wallet.publicKey || !projectAddress || isLoading}
              className="px-4 py-2 bg-teal-500 text-white rounded hover:bg-teal-600 disabled:bg-gray-400"
            >
              {isLoading ? "Checking..." : "Check Profile Exists"}
            </button>

            <button
              onClick={getAllProjects}
              disabled={!wallet.publicKey || isLoading}
              className="px-4 py-2 bg-indigo-500 text-white rounded hover:bg-indigo-600 disabled:bg-gray-400"
            >
              {isLoading ? "Getting..." : "Get My Projects"}
            </button>

            <button
              onClick={getProjectResources}
              disabled={!wallet.publicKey || !projectAddress || isLoading}
              className="px-4 py-2 bg-amber-500 text-white rounded hover:bg-amber-600 disabled:bg-gray-400"
            >
              {isLoading ? "Getting..." : "Get Project Resources"}
            </button>
          </div>
        </div>

        {response && (
          <div className="mt-8 p-6 bg-gray-700 rounded-lg">
            <h3 className="font-bold text-lg mb-3">Latest Transaction Result:</h3>
            <pre className="text-xs overflow-auto bg-gray-600 p-4 rounded border max-h-96">
              {JSON.stringify(response, null, 2)}
            </pre>
          </div>
        )}

        <div className="mt-8 p-6 bg-gray-500 rounded-lg">
          <h3 className="font-bold text-lg mb-3">Complete Setup Flow:</h3>
          <ol className="list-decimal list-inside space-y-2 text-sm">
            <li><strong>Create Project</strong> - Sets up your Honeycomb project</li>
            <li><strong>Create Profiles Tree</strong> - Sets up compressed storage for user profiles</li>
            <li><strong>Create Points Resource</strong> - Creates a points/currency system for your project</li>
            <li><strong>Create Resource Tree</strong> - Sets up compressed storage for the points resource</li>
            <li><strong>Create User</strong> - Creates universal user account (one per person)</li>
            <li><strong>Create User + Profile</strong> - Alternative: Creates both user and project-specific profile in one transaction</li>
          </ol>
          <div className="mt-4 p-4 bg-gray-600 rounded">
            <h4 className="font-bold mb-2">🎮 How the Points System Works:</h4>
            <ul className="text-sm space-y-1">
              <li>• <strong>Resource:</strong> Defines what "Points" are (like creating a currency type)</li>
              <li>• <strong>Resource Tree:</strong> Compressed storage system for efficient point tracking</li>
              <li>• <strong>Mint:</strong> Actually creates points and gives them to users when they win</li>
              <li>• <strong>Win Reward:</strong> Players get 10 points when they win a game</li>
              <li>• <strong>Balance Check:</strong> See how many points a user has accumulated</li>
            </ul>
          </div>
          <div className="mt-4 p-4 bg-blue-600 rounded">
            <h4 className="font-bold mb-2">🔧 Troubleshooting & Debug Features:</h4>
            <ul className="text-sm space-y-1">
              <li>• <strong>Debug Resources:</strong> Shows all resources in your current project</li>
              <li>• <strong>Enhanced Balance Check:</strong> Compares expected vs actual resource addresses</li>
              <li>• <strong>Console Logging:</strong> Detailed logs in browser console for debugging</li>
              <li>• <strong>Auto-Balance Check:</strong> Automatically checks balance after minting points</li>
              <li>• <strong>Error Handling:</strong> Clear error messages for common issues</li>
            </ul>
          </div>
          <div className="mt-4 p-4 bg-green-600 rounded">
            <h4 className="font-bold mb-2">📋 Current Configuration:</h4>
            <ul className="text-sm space-y-1">
              <li>• <strong>Project:</strong> {projectAddress}</li>
              <li>• <strong>Points Resource:</strong> {pointsResourceAddress}</li>
              <li>• <strong>Resource Tree:</strong> {resourceTreeAddress}</li>
              <li>• <strong>Connected Wallet:</strong> {wallet.publicKey?.toString() || "Not connected"}</li>
              <li>• <strong>Points Per Win:</strong> 10 PTS</li>
              <li>• <strong>Storage Type:</strong> Compressed (LedgerState)</li>
            </ul>
          </div>
          <div className="mt-4 p-4 bg-yellow-600 rounded">
            <h4 className="font-bold mb-2">🚀 What You Can Do Now:</h4>
            <ul className="text-sm space-y-1">
              <li>• <strong>Test the system:</strong> Click "Win Game" to mint 10 points to yourself</li>
              <li>• <strong>Check balances:</strong> Use "Check My Points" to see your current balance</li>
              <li>• <strong>Mint to others:</strong> Modify the code to give points to different wallet addresses</li>
              <li>• <strong>Scale up:</strong> Create additional resources (XP, coins, gems, etc.)</li>
              <li>• <strong>Build gameplay:</strong> Integrate this into your actual game logic</li>
              <li>• <strong>Add features:</strong> Implement point spending, transfers, or leaderboards</li>
            </ul>
          </div>
          <div className="mt-4 p-4 bg-red-600 rounded">
            <h4 className="font-bold mb-2">⚠️ Important Notes:</h4>
            <ul className="text-sm space-y-1">
              <li>• <strong>Project Scope:</strong> Resources belong to specific projects - can't mix and match</li>
              <li>• <strong>Authority Required:</strong> Only project authority can mint resources</li>
              <li>• <strong>Compressed Storage:</strong> Using   1. Resource: BqnYp7MLyi9rPCQ9Wb3pWmDZEdFv7LWu6EEwoiJQgpwc page.tsx:379:16
     Balance: 70 page.tsx:380:16
     Is Points Resource? false page.tsx:381:16
     ---Honeycomb's compression saves 1000x on costs</li>
              <li>• <strong>Solana Network:</strong> All transactions happen on Solana blockchain</li>
              <li>• <strong>Real Value:</strong> These are actual blockchain tokens, not just database entries</li>
              <li>• <strong>Wallet Connection:</strong> Users need a Solana wallet (Phantom, Solflare, etc.)</li>
            </ul>
          </div>
        </div>
      </div>
    </main>
  )
}