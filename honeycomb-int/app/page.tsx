'use client'

import React, { useState } from "react";
import { useWallet } from "@solana/wallet-adapter-react";
import { sendClientTransactions } from "@honeycomb-protocol/edge-client/client/walletHelpers";
import { client, PROJECT_ADDRESS } from "../constants/client";

export default function Home() {
  const wallet = useWallet();
  const [isLoading, setIsLoading] = useState(false);
  const [response, setResponse] = useState<any>(null);
  const [projectAddress, setProjectAddress] = useState(PROJECT_ADDRESS);

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
        authority: wallet.publicKey.toString(),
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
        project: projectAddress,
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
        project: projectAddress,
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
        authority: wallet.publicKey.toString()
      });
      console.log("Projects query result:", projects);
    } catch (error) {
      console.error("Error getting projects:", error);
      setResponse({
        type: "Projects Query",
        error: (error as Error).message,
        authority: wallet.publicKey.toString()
      });
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <main className="flex min-h-screen flex-col items-center p-8">
      <div className="w-full max-w-4xl">
        <h1 className="text-4xl font-bold text-center mb-8">Honeycomb Integration</h1>

        {projectAddress && (
          <div className="mb-6 p-4 bg-green-100 rounded">
            <h3 className="font-bold text-green-800">Project Address:</h3>
            <p className="text-sm text-green-700 font-mono">{projectAddress}</p>
          </div>
        )}

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-8">
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

          {/* Step 3: Create User Only */}
          <div className="p-6 border rounded-lg">
            <h2 className="text-xl font-bold mb-3">3. Create User</h2>
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

          {/* Step 4: Create User + Profile */}
          <div className="p-6 border rounded-lg">
            <h2 className="text-xl font-bold mb-3">4. Create User + Profile</h2>
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

        {/* Verification Section */}
        <div className="mb-8 p-6 bg-gray-800 rounded-lg">
          <h2 className="text-xl font-bold mb-4">🔍 Verification & Queries</h2>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
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
          <h3 className="font-bold text-lg mb-3">Setup Flow:</h3>
          <ol className="list-decimal list-inside space-y-2 text-sm">
            <li><strong>Create Project</strong> - Sets up your Honeycomb project</li>
            <li><strong>Create Profiles Tree</strong> - Sets up compressed storage for user profiles</li>
            <li><strong>Create User</strong> - Creates universal user account (one per person)</li>
            <li><strong>Create User + Profile</strong> - Alternative: Creates both user and project-specific profile in one transaction</li>
          </ol>
        </div>
      </div>
    </main>
  )
}